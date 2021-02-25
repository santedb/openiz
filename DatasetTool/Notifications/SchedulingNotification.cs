using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Data;
using MARC.HI.EHRS.SVC.Core.Services;
using OpenIZ.Core.Model.Entities;
using OpenIZ.Core.Security;
using OpenIZ.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenIZ.Core.Model;
using OpenIZ.Core.Scheduling;
using Newtonsoft.Json;
using MARC.HI.EHRS.SVC.Core.Services.Security;
using OpenIZ.Core.Notifications;
using OpenIZ.Core.Model.Constants;
using System.Diagnostics;

namespace OizDevTool.Notifications
{
    /// <summary>
    /// Scheduling / load balance notification process
    /// </summary>
    public class SchedulingNotification : INotificationProcess
    {
        /// <summary>
        /// Execute the process
        /// </summary>
        public void Process(DateTime refDate, string facilityId, string language)
        {

            try
            {

                // Here the date is the date we want messages sent, so we add days
                DateTime fromDate = refDate.AddDays(2).Date,
                    toDate = refDate.AddDays(3).Date;
                var timezone = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
                var ctz = $"{(timezone.Hours < 0 ? "" : "+")}{timezone.Hours:00}:{timezone.Minutes:00}";
                

                var warehouseService = ApplicationContext.Current.GetService<IAdHocDatawarehouseService>();
                var placeService = ApplicationContext.Current.GetService<IDataPersistenceService<Place>>() as IStoredQueryDataPersistenceService<Place>;
                var productService = ApplicationContext.Current.GetService<IDataPersistenceService<Material>>() as IStoredQueryDataPersistenceService<Material>;
                var userService = ApplicationContext.Current.GetService<IDataPersistenceService<UserEntity>>();
                var dataMart = warehouseService.GetDatamart("oizcp");
                var roleService = ApplicationContext.Current.GetService<IRoleProviderService>();
                var securityService = ApplicationContext.Current.GetService<ISecurityRepositoryService>();

                Console.WriteLine("Fetching planned events from {0:o} and {1:o} in TZ {2}", fromDate.Date, toDate.Date,ctz);

                var query = String.Format(@"SELECT patient_id, given, alt_id, coalesce(pat_vw.tel, mth_tel, nok_tel) as tel, location_id, array_agg(product_id) as product, array_agg(act_date) as dates, fac_name
                                    FROM
	                                    oizcp
	                                    INNER JOIN pat_vw ON (patient_id = pat_id)
	                                    INNER JOIN fac_vw ON (fac_vw.fac_id = location_id)
                                    WHERE
                                        -- BETWEEN START
	                                    act_date BETWEEN '{0:o}' AND '{1:o}'
                                        -- BETWEEN END
	                                    AND class_id = '932a3c7e-ad77-450a-8a1f-030fc2855450'
	                                    AND NOT EXISTS (SELECT TRUE FROM sbadm_tbl WHERE pat_id = patient_id AND mat_id = product_id AND seq_id = dose_seq AND COALESCE(neg_ind, FALSE) = FALSE AND (sbadm_tbl.act_utc at time zone '+03:00')::DATE > '{0:o}'::DATE - '1 MONTH'::INTERVAL )
                                    GROUP BY patient_id, alt_id, coalesce(pat_vw.tel, mth_tel, nok_tel), given,  location_id, fac_name", fromDate, toDate, ctz);
                if (!String.IsNullOrEmpty(facilityId))
                    query = String.Format(@"SELECT patient_id, given, alt_id, coalesce(pat_vw.tel, mth_tel, nok_tel) as tel, location_id, array_agg(product_id) as product, array_agg(act_date) as dates, fac_name
                                    FROM
	                                    oizcp
	                                    INNER JOIN pat_vw ON (patient_id = pat_id)
	                                    INNER JOIN fac_vw ON (fac_vw.fac_id = location_id)
                                    WHERE
                                        -- BETWEEN START
	                                    act_date BETWEEN '{0:o}' AND '{1:o}'
                                        AND location_id = '{2}'
                                        -- BETWEEN END
	                                    AND class_id = '932a3c7e-ad77-450a-8a1f-030fc2855450'
	                                    AND NOT EXISTS (SELECT TRUE FROM sbadm_tbl WHERE pat_id = patient_id AND mat_id = product_id AND seq_id = dose_seq AND COALESCE(neg_ind, FALSE) = FALSE  AND (sbadm_tbl.act_utc at time zone '+03:00')::DATE > '{0:o}'::DATE - '1 MONTH'::INTERVAL )
                                    GROUP BY patient_id, alt_id, coalesce(pat_vw.tel, mth_tel, nok_tel), given,  location_id, fac_name", fromDate, toDate, facilityId, ctz);
                var plannedEvents = warehouseService.AdhocQuery(query);

                Console.WriteLine("There are {0} planned events which can receive messages for this timeframe", plannedEvents.Count());

                Dictionary<Guid, Material> productRefs = new Dictionary<Guid, Material>();
                foreach (var itm in productService.Query(o => o.ClassConceptKey == EntityClassKeys.Material && o.TypeConcept.Mnemonic.Contains("VaccineType-*"), AuthenticationContext.SystemPrincipal))
                {
                    if (!productRefs.ContainsKey(itm.Key.Value))
                    {
                        Console.WriteLine("{0} => {1}", itm.Key, itm.Names.First().Component.First().Value);
                        productRefs.Add(itm.Key.Value, itm);
                    }
                }

                List<dynamic> problemAppointments = new List<dynamic>();

                // Create buckets of time for the specified places
                var locations = plannedEvents.GroupBy(o => o.location_id);
                foreach (var loc in locations)
                {
                    var placeData = placeService.Get(new Identifier<Guid>((Guid)loc.Key, null), AuthenticationContext.SystemPrincipal, true);
                    if (placeData == null)
                    {
                        Console.WriteLine("Skipping location {0}", loc.First().fac_name);
                        continue;
                    }
                    Console.WriteLine("{0} events for location {1}", loc.Count(), loc.First().fac_name);

                    // Load the capacity schedule
                    var serviceInformation = placeData.LoadCollection<PlaceService>(nameof(Place.Services)).FirstOrDefault(o => o.ServiceConceptKey == Guid.Parse("f5304ED0-6C9F-411B-B008-A1E1561B7963"));
                    ClinicServiceScheduleInfo capacityConfiguration = null;

                    if (serviceInformation == null)
                    {
                        // HACK: Update and re-query for all events this week and use that as the default for the plan
                        Console.WriteLine("\tThis facility does not have a capacity schedule - will use weekly plan");
                        int spos = query.IndexOf("-- BETWEEN START"), epos = query.IndexOf("-- BETWEEN END");
                        query = query.Substring(0, spos) +
                            $"act_date BETWEEN '{fromDate:o}'::TIMESTAMP AND '{toDate:o}'::TIMESTAMP + '5 DAY'::INTERVAL " +
                            $" AND location_id = '{loc.Key}'" +
                            query.Substring(epos + 14);

                        var count = warehouseService.AdhocQuery($"SELECT COUNT(DISTINCT patient_id) AS C FROM ({query}) i").FirstOrDefault();
                        if (count.c < 200)
                            capacityConfiguration = new ClinicServiceScheduleInfo()
                            {
                                Schedule = new List<ClinicScheduleInfo>()
                                {
                                    new ClinicScheduleInfo()
                                    {
                                        Capacity = 40,
                                        Days = new List<DayOfWeek>() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
                                        StartXml = "09:00",
                                        StopXml = "15:30"
                                    }
                                }
                            };
                        else
                            // Balance the load out over the week 
                            capacityConfiguration = new ClinicServiceScheduleInfo()
                            {
                                Schedule = new List<ClinicScheduleInfo>()
                                {
                                    new ClinicScheduleInfo()
                                    {
                                        Capacity = (int)count.c / (int)5,
                                        Days = new List<DayOfWeek>() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
                                        StartXml = "09:00",
                                        StopXml = "15:30"
                                    }
                                }
                            };

                        
                    }
                    else 
                        capacityConfiguration = JsonConvert.DeserializeObject<ClinicServiceScheduleInfo>(serviceInformation.ServiceSchedule);
                    var scheduledSlots = new Dictionary<DateTime, Int32>();
                    for (var d = 0; d <= toDate.Subtract(fromDate).TotalDays + 7; d++)
                    {
                        var date = fromDate.AddDays(d);
                        var capacity = capacityConfiguration.Schedule.FirstOrDefault(o => o.Days.Contains(date.DayOfWeek));
                        if (capacity == null || capacity.Capacity == 0)
                            continue;

                        var openHours = capacity.Stop.Value.TimeOfDay.Subtract(capacity.Start.Value.TimeOfDay).TotalHours;
                        for (var t = 0; t < openHours; t++)
                        {
                            // Remove suggested appointments already sent from capacity
                            var slotStart = date.Date.Add(capacity.Start.Value.TimeOfDay).AddHours(t);
                            var alreadySent = warehouseService.AdhocQuery(String.Format(@"SELECT msg_id FROM appt_slot_tbl WHERE appt_dt BETWEEN '{0:yyyy-MM-ddTHH:mm}' AND '{1:yyyy-MM-ddTHH:mm}' AND plc_id = '{2}';", slotStart, slotStart.AddHours(1), loc.Key));
                            var cap = (int)Math.Round((float)capacity.Capacity / openHours, MidpointRounding.AwayFromZero) - alreadySent.Count;
                            scheduledSlots.Add(slotStart, cap);
                        }
                    }

                    // Process the appointments
                    foreach (var appt in loc)
                    {
                        if(DBNull.Value.Equals(appt.tel) || String.IsNullOrEmpty((String)appt.tel))
                        {
                            Console.WriteLine($"Skipping notification for {appt.alt_id} as this patient has no contact information");
                            Trace.TraceWarning($"Skipping notification for {appt.alt_id} as this patient has no contact information");
                            continue;
                        }

                        // First pre-req data for this patient
                        var products = new List<Guid>(appt.product).Select(o =>
                        {
                            try
                            {
                                return productRefs[o];
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Error referencing {0} - {1}", o, e.Message);
                                throw;
                            }
                        });
                        var scheduleDate = new List<DateTime>(appt.dates).First();

                        // Next we want to find an available time slot
                        for (var d = -3; d < 4; d++)
                        {
                            var testDate = scheduleDate.AddDays(d);
                            var availableSlot = scheduledSlots.FirstOrDefault(o => o.Key.Date == testDate.Date && o.Value > 0);
                            if (availableSlot.Key != DateTime.MinValue)
                            {
                                scheduleDate = availableSlot.Key;
                                scheduledSlots[scheduleDate]--;
                                break;
                            }

                            // TODO: Clean this up
                            if (d == 3) // Couldn't find a slot :/ 
                            {
                                var notif = userService.Query(o => o.Relationships.Where(g => g.RelationshipType.Mnemonic == "DedicatedServiceDeliveryLocation").Any(r => r.TargetEntityKey == placeData.Key), AuthenticationContext.SystemPrincipal);
                                problemAppointments.Add(appt);
                                // Prepare template
                                var templateValues = new Dictionary<string, String>()
                                    {
                                        { "given", appt.given.ToString() },
                                        { "product", NotificationUtils.GetProductList(products, language) },
                                        { "fac_name", appt.fac_name.ToString() },
                                        { "barcode", appt.alt_id }
                                    }; 
                                foreach (var user in notif)
                                {
                                    var tel = user.SecurityUser?.PhoneNumber;
                                    if (!String.IsNullOrEmpty(tel))
                                        NotificationUtils.SendNotification("ProblemNotification", tel, "Problem Booking", scheduleDate.AddDays(-1), templateValues);
                                }

                            }
                        }
                        if (scheduleDate.TimeOfDay.Hours == 0)
                            continue;

                        var alreadySent = warehouseService.AdhocQuery(String.Format(@"SELECT msg_id FROM appt_slot_tbl WHERE appt_dt::DATE = '{0:yyyy-MM-dd}' AND plc_id = '{1}' AND pat_id = '{2}';", scheduleDate.Date, loc.Key, appt.patient_id));
                        if (alreadySent.Count > 0)
                        {
                            Console.WriteLine("Notification already sent for timeslot for {0}", appt.patient_id);
                            continue;
                        }

                        // Prepare template
                        Dictionary<String, String> templateFields = new Dictionary<string, String>()
                        {
                            { "given", appt.given.ToString() },
                            { "product", NotificationUtils.GetProductList(products, language) },
                            { "fac_name", appt.fac_name.ToString() },
                            { "from", scheduleDate.ToString("HH:mm") },
                            { "to", scheduleDate.AddHours(1).ToString("HH:mm") },
                            { "date", scheduleDate.Date.ToString("ddd dd MMM, yyyy") }
                        };

                        var msgId = NotificationUtils.SendNotification("AppointmentNotification", appt.tel, "Appointment Notification", scheduleDate.AddDays(-1), templateFields); // NotificationRelayUtil.GetNotificationRelay($"tel:{appt.tel}").Send($"tel:{appt.tel}", "Appointment Reminder", body, scheduleDate.AddDays(-1));
                        Guid slotId = Guid.NewGuid();
                        warehouseService.AdhocQuery(String.Format("INSERT INTO APPT_SLOT_TBL (APPT_SLOT_ID, MSG_ID, PAT_ID, APPT_DT, PLC_ID) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", slotId, msgId, appt.patient_id, scheduleDate, loc.Key));
                        warehouseService.AdhocQuery(String.Format("INSERT INTO MSG_QUEUE_LOG_TBL (MSG_ID, PAT_ID, PROC_NAME, REF_ID) VALUES ('{0}', '{1}', '{2}', '{3}')", msgId, appt.patient_id, nameof(SchedulingNotification), slotId));

                    }
                }

                // Notify administrators of the issues
                if (problemAppointments.Any())
                {

                    var problemBody = String.Join(",", (problemAppointments.First() as IDictionary<String, Object>).Keys) + "\r\n";
                    problemBody += String.Join("\r\n", problemAppointments.OfType<IDictionary<String, Object>>().Select(o => String.Join(",", o.Values.Select(v => v is Guid[]? String.Join(";", (v as Guid[]).Select(p => productRefs[p].Names.Last())) : v is DateTime[]? String.Join(";", v as DateTime[]) : v.ToString()))));

                    // foreach(var rcpt in adminUsers.Select(o=>o.Email).Distinct())
                    AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);
                    var adminUsers = roleService.FindUsersInRole("NOTIFIED_USERS").Select(o => ApplicationContext.Current.GetService<ISecurityRepositoryService>().GetUser(o).Email);
                    foreach (var rcpt in adminUsers.Select(o => securityService.GetUser(o)).Distinct())
                        NotificationRelayUtil.GetNotificationRelay($"mailto:{rcpt}")?.Send($"mailto:{rcpt}", "Care Plan Reminder Service - Unschedulable Appointments", "Some planned appointments could not be scheduled in the allotted time for some facilities, see attachment", null, new Dictionary<String, String>() { { "problem-appointments.csv", problemBody } });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error sending notifications: {0}", e);
            }
        }
    }
}
