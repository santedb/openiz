﻿/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: fyfej
 * Date: 2017-9-1
 */
using MARC.Everest.Threading;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using MohawkCollege.Util.Console.Parameters;
using OpenIZ.Core;
using OpenIZ.Core.Data.Warehouse;
using OpenIZ.Core.Extensions;
using OpenIZ.Core.Model.Acts;
using OpenIZ.Core.Model.Constants;
using OpenIZ.Core.Model.DataTypes;
using OpenIZ.Core.Model.Entities;
using OpenIZ.Core.Model.EntityLoader;
using OpenIZ.Core.Model.Roles;
using OpenIZ.Core.Security;
using OpenIZ.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenIZ.Core.Model;
using System.Collections.Specialized;
using MARC.HI.EHRS.SVC.Core.Data;
using OpenIZ.Core.Query;
using OpenIZ.Caching.Memory;
using OpenIZ.Core.Scheduling;
using OpenIZ.Core.Scheduling.Impl;
using Newtonsoft.Json;
using System.IO;
using OpenIZ.Core.Notifications;
using System.Dynamic;
using OpenIZ.Core.Model.Security;
using MARC.HI.EHRS.SVC.Core.Services.Security;
using OizDevTool.Notifications;

namespace OizDevTool
{
    /// <summary>
    /// Represents functions related to warehousing
    /// </summary>
    [Description("Warehouse related functions")]
    public static class Warehouse
    {

        /// <summary>
        /// Careplan parameters
        /// </summary>
        public class CareplanParameters
        {
            [Parameter("trunc")]
            [Description("Truncates (deletes) all calculated plans")]
            public bool Truncate { get; set; }

            [Parameter("create")]
            [Description("Create the warehouse tables if they don't exist")]
            public bool Create { get; set; }

            [Parameter("since")]
            [Description("Create plan for all those records modified since")]
            public string Since { get; set; }

            [Parameter("timespan")]
            [Description("Only process for those on certain timespan")]
            public string Timespan { get; set; }

            [Parameter("nofulfill")]
            [Description("Does not scan for fulfillments from acts")]
            public bool NoFulfill { get; set; }

            [Parameter("fulfill")]
            [Description("Calculate fulfillment for the specified type of act")]
            public StringCollection ActTypes { get; set; }

            [Parameter("patient")]
            [Description("Calculate care plan for specified patients")]
            public StringCollection PatientId { get; set; }

            [Parameter("facility")]
            [Description("Calculate care plan for the specified facility")]
            public String FacilityId { get; set; }

            [Parameter("limit")]
            [Description("Limit number of processing items")]
            public String Limit { get; set; }

            [Parameter("skip")]
            [Description("Skip number of processing items")]
            public String Skip { get; set; }

            [Parameter("concurrency")]
            [Description("Sets the number of concurrent plans to generate")]
            public String Concurrency { get; set; }

            [Parameter("missing")]
            [Description("Indicates that the care planner should only calculate for patients who don't have a care plan")]
            public bool Missing { get; set; }

        }

        /// <summary>
        /// Notification parameters
        /// </summary>
        public class NotifyParameters
        {
            [Parameter("date")]
            [Description("The date to run process as (default: Now)")]
            public string Date { get; set; }

            [Parameter("facility")]
            [Description("Calculate care plan for the specified facility")]
            public StringCollection FacilityId { get; set; }

            [Parameter("lang")]
            [Description("Sets the language of the messages")]
            public String Language { get; set; }

        }

        /// <summary>
        /// AMC parameters
        /// </summary>
        public class AmcParameters
        {
            [Parameter("name")]
            [Description("Name of the facility to calculate")]
            public string Name { get; set; }

        }

        /// <summary>
        /// Schedule parameters
        /// </summary>
        public class ScheduleParameters
        {

            /// <summary>
            /// Minimum capacity
            /// </summary>
            [Parameter("min-cap")]
            [Description("Set the minimum capacity")]
            public String MinCapacity { get; set; }

            [Parameter("name")]
            [Description("Name of facility to set schedule for")]
            public string Name { get; set; }

            [Parameter("open")]
            [Description("The start of the day when clinics should open")]
            public String OpenTime { get; set; }

            [Parameter("close")]
            [Description("The stop time when clinics close")]
            public String CloseTime { get; set; }

        }

        /// <summary>
        /// Calculates the AMC for all facilities in the system
        /// </summary>
        [Description("Calculate Average Monthly Consumption (based on 3 months of data)")]
        [ParameterClass(typeof(AmcParameters))]
        public static int Amc(string[] args)
        {
            var parms = new ParameterParser<AmcParameters>().Parse(args);
            ApplicationContext.Current.Start();
            ApplicationServiceContext.Current = ApplicationContext.Current;
            AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);
            EntitySource.Current = new EntitySource(new PersistenceServiceEntitySource());

            // Get the place service
            var placeService = ApplicationContext.Current.GetService<IDataPersistenceService<Place>>() as IStoredQueryDataPersistenceService<Place>;
            var apService = ApplicationContext.Current.GetService<IDataPersistenceService<ActParticipation>>();
            var matlService = ApplicationContext.Current.GetService<IDataPersistenceService<Material>>() as IFastQueryDataPersistenceService<Material>;
            var erService = ApplicationContext.Current.GetService<IDataPersistenceService<EntityRelationship>>() as IFastQueryDataPersistenceService<EntityRelationship>;
            DateTime startDate = DateTime.Now.AddMonths(-3);

            Dictionary<Guid, Guid> manufacturedMaterialLinks = new Dictionary<Guid, Guid>();
            Dictionary<Guid, Int32> quantityInfo = new Dictionary<Guid, int>();

            int tr = 1, ofs = 0;
            Guid queryId = Guid.NewGuid();
            while (ofs < tr)
            {
                Console.WriteLine("Fetch facilities: {0} - {1} of {2}", ofs, ofs + 100, tr); ;

                IEnumerable<Place> places = null;
                if (String.IsNullOrEmpty(parms.Name))
                    places = placeService.Query(o => o.ClassConceptKey == EntityClassKeys.ServiceDeliveryLocation, queryId, ofs, 100, AuthenticationContext.Current.Principal, out tr);
                else
                    places = placeService.Query(o => o.ClassConceptKey == EntityClassKeys.ServiceDeliveryLocation && o.Names.Any(n => n.Component.Any(c => c.Value.Contains(parms.Name))), ofs, 100, AuthenticationContext.Current.Principal, out tr);

                foreach (var plc in places)
                {
                    try
                    {
                        Console.WriteLine("Calculating AMC for {0} ({1})", plc.Names.FirstOrDefault().ToDisplay(), plc.Key);

                        // First we want to get all entity relationships of type consumable related to this place
                        int tc = 0;
                        var consumablePtcpts = apService.Query(o => o.ParticipationRoleKey == ActParticipationKey.Consumable && o.SourceEntity.ActTime > startDate && o.SourceEntity.Participations.Any(p => p.ParticipationRoleKey == ActParticipationKey.Location && p.PlayerEntityKey == plc.Key) && !o.SourceEntity.Tags.Any(tg => tg.TagKey == "excludeFromLedger"), 0, 100000, AuthenticationContext.Current.Principal, out tc);
                        Console.WriteLine("\tFetch {0} events since {1:o}...", tc, startDate);

                        // Now we want to group by consumable
                        int t = 0;

                        var groupedConsumables = consumablePtcpts.GroupBy(o => o.PlayerEntityKey).Select(o =>
                        {

                            Guid matKey = Guid.Empty;
                            if (!manufacturedMaterialLinks.TryGetValue(o.Key.Value, out matKey))
                            {
                                matKey = matlService.QueryFast(m => m.Relationships.Any(r => r.RelationshipTypeKey == EntityRelationshipTypeKeys.Instance && r.TargetEntityKey == o.Key), Guid.Empty, 0, 1, AuthenticationContext.Current.Principal, out t).FirstOrDefault()?.Key.Value ?? Guid.Empty;
                                lock (manufacturedMaterialLinks)
                                    if (!manufacturedMaterialLinks.ContainsKey(o.Key.Value))
                                        manufacturedMaterialLinks.Add(o.Key.Value, matKey);
                            }

                            return new
                            {
                                ManufacturedMaterialKey = o.Key,
                                UsedQty = o.Sum(s => s.Quantity),
                                MaterialKey = matKey
                            };
                        }).ToList();

                        foreach (var i in groupedConsumables.Where(o => o.MaterialKey == Guid.Empty))
                            Console.WriteLine("MMAT {0} is not linked to any MAT", i.ManufacturedMaterialKey);

                        groupedConsumables.RemoveAll(o => o.MaterialKey == Guid.Empty);
                        // Now, we want to build the stock policy object
                        dynamic[] stockPolicyObject = new dynamic[0];
                        var stockPolicyExtension = plc.LoadCollection<EntityExtension>("Extensions").FirstOrDefault(o => o.ExtensionTypeKey == Guid.Parse("DFCA3C81-A3C4-4C82-A901-8BC576DA307C"));
                        if (stockPolicyExtension == null)
                        {
                            stockPolicyExtension = new EntityExtension()
                            {
                                ExtensionType = new ExtensionType("http://openiz.org/extensions/contrib/bid/stockPolicy", typeof(DictionaryExtensionHandler))
                                {
                                    Key = Guid.Parse("DFCA3C81-A3C4-4C82-A901-8BC576DA307C")
                                },
                                ExtensionValue = stockPolicyObject
                            };
                            plc.Extensions.Add(stockPolicyExtension);
                        }
                        else
                            stockPolicyObject = (stockPolicyExtension.GetValue() as dynamic[]).GroupBy(o => o["MaterialEntityId"]).Select(o => new
                            {
                                MaterialEntityId = Guid.Parse(o.FirstOrDefault()["MaterialEntityId"].ToString()),
                                ReorderQuantity = (int?)(o.FirstOrDefault()["ReorderQuantity"]) ?? 0,
                                SafetyQuantity = (int?)(o.FirstOrDefault()["SafetyQuantity"]) ?? 0,
                                AMC = (int?)(o.FirstOrDefault()["AMC"]) ?? 0,
                                Multiplier = (int?)(o.FirstOrDefault()["Multiplier"]) ?? 0
                            }).ToArray();



                        // Now we want to calculate each amc
                        List<dynamic> calculatedStockPolicyObject = new List<dynamic>();
                        bool hasChanged = false;
                        foreach (var gkp in groupedConsumables.GroupBy(o => o.MaterialKey).Select(o => new { Key = o.Key, Value = o.Sum(p => p.UsedQty) }))
                        {

                            var amc = (int)((float)Math.Abs(gkp.Value ?? 0) / 3);
                            // Now correct for packaging

                            int presentation = 0;
                            if (!quantityInfo.TryGetValue(gkp.Key, out presentation))
                            {
                                var pkging = erService.Query(o => o.SourceEntityKey == gkp.Key && o.RelationshipTypeKey == EntityRelationshipTypeKeys.Instance, AuthenticationContext.Current.Principal).Max(o => o.Quantity);
                                presentation = pkging.Value;
                                quantityInfo.Add(gkp.Key, pkging.Value);
                            }

                            if (presentation > 1)
                                amc = ((amc / presentation) + 1) * presentation;

                            Console.WriteLine("\tAMC for {0} after packaging {1}...", gkp.Key, amc);


                            // Is there an existing stock policy object?
                            var existingPolicy = stockPolicyObject.FirstOrDefault(o => o.MaterialEntityId == gkp.Key);
                            hasChanged |= amc != existingPolicy?.AMC;
                            if (existingPolicy != null && amc != existingPolicy?.AMC)
                                existingPolicy = new
                                {
                                    MaterialEntityId = gkp.Key,
                                    ReorderQuantity = existingPolicy.ReorderQuantity,
                                    SafetyQuantity = existingPolicy.SafetyQuantity,
                                    AMC = amc,
                                    Multiplier = existingPolicy.Multiplier
                                };
                            else
                                existingPolicy = new
                                {
                                    MaterialEntityId = gkp.Key,
                                    ReorderQuantity = amc,
                                    SafetyQuantity = (int)(amc * 0.33),
                                    AMC = amc,
                                    Multiplier = 1
                                };

                            // add policy
                            calculatedStockPolicyObject.Add(existingPolicy);
                        }

                        stockPolicyExtension.ExtensionValue = calculatedStockPolicyObject.ToArray();

                        if (hasChanged)
                            placeService.Update(plc, AuthenticationContext.Current.Principal, TransactionMode.Commit);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }

                ofs += 100;
            }


            return 1;
        }

        /// <summary>
        /// Calculates the AMC for all facilities in the system
        /// </summary>
        [Description("Overwrite schedules with calculated values")]
        [ParameterClass(typeof(ScheduleParameters))]
        public static int DefaultSchedule(string[] args)
        {
            var parms = new ParameterParser<ScheduleParameters>().Parse(args);
            ApplicationContext.Current.Start();
            ApplicationServiceContext.Current = ApplicationContext.Current;
            AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);
            EntitySource.Current = new EntitySource(new PersistenceServiceEntitySource());

            // Get the place service
            var placeService = ApplicationContext.Current.GetService<IDataPersistenceService<Place>>() as IStoredQueryDataPersistenceService<Place>;
            var visitService = ApplicationContext.Current.GetService<IDataPersistenceService<PatientEncounter>>();
            var scheduleService = new DefaultClinicScheduleService();


            DateTime startDate = DateTime.Now.AddMonths(-1);

            int weekDays = 0, minCap = 0;
            for (int d = 0; d < DateTime.Now.Subtract(startDate).TotalDays; d++)
            {
                var testDate = startDate.AddDays(d);
                if (testDate.DayOfWeek > DayOfWeek.Sunday && testDate.DayOfWeek < DayOfWeek.Saturday) weekDays++;
            }

            if (!String.IsNullOrEmpty(parms.MinCapacity))
                minCap = Int32.Parse(parms.MinCapacity);

            int tr = 1, ofs = 0;
            Guid queryId = Guid.NewGuid();
            while (ofs < tr)
            {
                Console.WriteLine("Fetch facilities: {0} - {1} of {2}", ofs, ofs + 100, tr); ;

                IEnumerable<Place> places = null;
                if (String.IsNullOrEmpty(parms.Name))
                    places = placeService.Query(o => o.ClassConceptKey == EntityClassKeys.ServiceDeliveryLocation, queryId, ofs, 100, AuthenticationContext.Current.Principal, out tr);
                else
                    places = placeService.Query(o => o.ClassConceptKey == EntityClassKeys.ServiceDeliveryLocation && o.Names.Any(n => n.Component.Any(c => c.Value.Contains(parms.Name))), ofs, 100, AuthenticationContext.Current.Principal, out tr);

                foreach (var plc in places)
                {
                    try
                    {
                        Console.WriteLine("Calculating Scheduled Capacity for {0} ({1})", plc.Names.FirstOrDefault().ToDisplay(), plc.Key);

                        // First we want to get all entity relationships of type consumable related to this place
                        var visitCount = visitService.Count(o => o.ActTime > startDate && o.Participations.Where(p => p.ParticipationRole.Mnemonic == "Location").Any(p => p.PlayerEntityKey == plc.Key), AuthenticationContext.Current.Principal);
                        var capacity = (float)visitCount / weekDays;
                        if (capacity < minCap)
                            capacity = minCap;
                        Console.WriteLine("\t{0} visits since {1:o} ({2} weekdays) = {3} visits / day...", visitCount, startDate, weekDays, capacity);

                        // Now create schedule
                        var schedule = plc.LoadCollection<PlaceService>(nameof(Place.Services)).FirstOrDefault(o => o.ServiceConceptKey == Guid.Parse("f5304ED0-6C9F-411B-B008-A1E1561B7963")) ??
                            new PlaceService()
                            {
                                SourceEntityKey = plc.Key,
                                ServiceConceptKey = Guid.Parse("f5304ED0-6C9F-411B-B008-A1E1561B7963"),
                                EffectiveVersionSequenceId = plc.VersionSequence
                            };

                        var clinicSchedule = new ClinicServiceScheduleInfo();
                        clinicSchedule.ServiceConceptKey = Guid.Parse("f5304ED0-6C9F-411B-B008-A1E1561B7963");
                        foreach (var d in Enum.GetValues(typeof(DayOfWeek)).OfType<DayOfWeek>())
                            switch (d)
                            {
                                case DayOfWeek.Saturday:
                                case DayOfWeek.Sunday:
                                    break;
                                default:
                                    clinicSchedule.Schedule.Add(new ClinicScheduleInfo()
                                    {
                                        Capacity = (int)Math.Round(capacity),
                                        Days = new List<DayOfWeek>()
                                        {
                                            d
                                        },
                                        StartXml = parms.OpenTime,
                                        StopXml = parms.CloseTime
                                    });
                                    break;
                            }


                        schedule.ServiceSchedule = JsonConvert.SerializeObject(clinicSchedule);

                        // Update or insert old schedule
                        if (schedule.Key.HasValue)
                            ApplicationContext.Current.GetService<IDataPersistenceService<PlaceService>>().Update(schedule, AuthenticationContext.SystemPrincipal, TransactionMode.Commit);
                        else
                            ApplicationContext.Current.GetService<IDataPersistenceService<PlaceService>>().Insert(schedule, AuthenticationContext.SystemPrincipal, TransactionMode.Commit);


                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }

                ofs += 100;
            }


            return 1;
        }

        /// <summary>
        /// Re-sets the original dates on all acts that don't have them
        /// </summary>
        [Description("Add the originalDate tag to acts which don't have them")]
        public static int AddOriginalDateTag(string[] argv)
        {
            ApplicationContext.Current.Start();
            ApplicationServiceContext.Current = ApplicationContext.Current;
            AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);
            EntitySource.Current = new EntitySource(new PersistenceServiceEntitySource());
            var warehouseService = ApplicationContext.Current.GetService<IAdHocDatawarehouseService>();
            var planService = ApplicationContext.Current.GetService<ICarePlanService>();
            var actPersistence = ApplicationContext.Current.GetService<IStoredQueryDataPersistenceService<SubstanceAdministration>>();
            var tagService = ApplicationContext.Current.GetService<ITagPersistenceService>();

            var oizcpDm = warehouseService.GetDatamart("oizcp");
            if (oizcpDm == null)
            {
                Console.WriteLine("OIZCP datamart does not exist!");
                return -1;
            }

            WaitThreadPool wtp = new WaitThreadPool();

            Guid queryId = Guid.NewGuid();
            int tr = 0, ofs = 0;
            var acts = actPersistence.Query(o => !o.Tags.Any(t => t.TagKey == "originalDate"), queryId, 0, 100, AuthenticationContext.SystemPrincipal, out tr);
            while (ofs < tr)
            {
                foreach (var itm in acts)
                {
                    wtp.QueueUserWorkItem((o) =>
                    {
                        var act = o as Act;

                        Console.WriteLine("Set originalDate for {0}", act.Key);

                        AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);

                        var actProtocol = act.LoadCollection<ActProtocol>("Protocols").FirstOrDefault();
                        if (actProtocol != null)
                        {
                            // Get the original date
                            var warehouseObj = warehouseService.AdhocQuery(oizcpDm.Id, new { protocol_id = actProtocol.ProtocolKey, sequence_id = actProtocol.Sequence });
                            if (warehouseObj.Any())
                            {
                                DateTime originalDate = warehouseObj.FirstOrDefault().act_date;
                                var originalEpochTime = (originalDate.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
                                tagService.Save(act.Key.Value, new ActTag("originalDate", originalEpochTime.ToString()));
                            }
                        }
                    }, itm);
                }
                ofs += 100;
                acts = actPersistence.Query(o => !o.Tags.Any(t => t.TagKey == "originalDate"), queryId, ofs, 100, AuthenticationContext.SystemPrincipal, out tr);

            }

            wtp.WaitOne();
            return 0;
        }

        /// <summary>
        /// Send notifications out
        /// </summary>
        [Description("Sends notifications out for the careplan based on capacity")]
        [ParameterClass(typeof(NotifyParameters))]
        public static int SendNotify(String[] argv)
        {

            var parms = new ParameterParser<NotifyParameters>().Parse(argv);
            ApplicationContext.Current.Start();
            ApplicationServiceContext.Current = ApplicationContext.Current;
            AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);
            EntitySource.Current = new EntitySource(new PersistenceServiceEntitySource());

            DateTime date = String.IsNullOrEmpty(parms.Date) ? DateTime.Now : DateTime.Parse(parms.Date).Date;

            // TODO: Make this reflection
            if (parms.FacilityId != null)
                foreach (var fac in parms.FacilityId)
                {
                    new SchedulingNotification().Process(date, fac, parms.Language);
                    new DefaultersNotification().Process(date, fac, parms.Language);
                }
            else
            {
                new SchedulingNotification().Process(date, String.Empty, parms.Language);
                new DefaultersNotification().Process(date, String.Empty, parms.Language);
            }
            return 0;

        }

        /// <summary>
        /// Generate a care plan
        /// </summary>
        [Description("Re-generates the data warehouse OIZCP plan for all patients")]
        [ParameterClass(typeof(CareplanParameters))]
        public static int Careplan(string[] argv)
        {
            var parms = new ParameterParser<CareplanParameters>().Parse(argv);
            ApplicationContext.Current.Start();
            ApplicationServiceContext.Current = ApplicationContext.Current;
            AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);
            EntitySource.Current = new EntitySource(new PersistenceServiceEntitySource());
            var warehouseService = ApplicationContext.Current.GetService<IAdHocDatawarehouseService>();
            var planService = ApplicationContext.Current.GetService<ICarePlanService>();

            if (warehouseService == null)
                throw new InvalidOperationException("Ad-hoc data warehouse service is not registered");
            if (planService == null)
                throw new InvalidOperationException("Missing care plan service");

            // Warehouse service
            foreach (var cp in planService.Protocols)
                Console.WriteLine("Loaded {0}...", cp.Name);

            // Deploy schema?
            var dataMart = warehouseService.GetDatamart("oizcp");
            if (dataMart == null)
            {
                if (parms.Create)
                {
                    Console.WriteLine("Datamart for care plan service doesn't exist, will have to create it...");
                    dataMart = warehouseService.CreateDatamart("oizcp", DatamartSchema.Load(typeof(Warehouse).Assembly.GetManifestResourceStream("OizDevTool.Resources.CarePlanWarehouseSchema.xml")));
                }
                else
                    throw new InvalidOperationException("Warehouse schema does not exist!");
            }

            // Truncate?
            if (parms.Truncate)
                warehouseService.Truncate(dataMart.Id);

            // Now we want to calculate
            var patientPersistence = ApplicationContext.Current.GetService<IStoredQueryDataPersistenceService<Patient>>();
            DateTimeOffset? lastRefresh =
                !String.IsNullOrEmpty(parms.Since) ? (DateTimeOffset?)DateTimeOffset.Parse(parms.Since) :
                !String.IsNullOrEmpty(parms.Timespan) ? (DateTimeOffset?)(DateTimeOffset.Now - TimeSpan.Parse(parms.Timespan)) :
                null;

            Console.WriteLine("Will only calculate care plans since : {0}", lastRefresh.GetValueOrDefault().ToString("o"));
            // Should we calculate?
            int tr = 1, ofs = 0, calc = 0, tq = 0;
            var warehousePatients = warehouseService.StoredQuery(dataMart.Id, "consistency", new { }, out tq);
            tq = 0;
            Guid queryId = Guid.NewGuid();
            WaitThreadPool wtp = new WaitThreadPool(String.IsNullOrEmpty(parms.Concurrency) ? Environment.ProcessorCount  : Int32.Parse(parms.Concurrency));
            DateTime start = DateTime.Now;

            // Type filters
            List<Guid> typeFilter = new List<Guid>();
            if (parms.ActTypes?.Count > 0)
            {
                var cpcr = ApplicationContext.Current.GetService<IConceptRepositoryService>();
                foreach (var itm in parms.ActTypes)
                    typeFilter.Add(cpcr.GetConcept(itm).Key.Value);
            }

            if (!String.IsNullOrEmpty(parms.FacilityId))
            {
                warehouseService.Delete(dataMart.Id, new { location_id = parms.FacilityId });
                warehousePatients = new List<dynamic>();
            }

            int limit = Int32.MaxValue;
            if (!String.IsNullOrEmpty(parms.Limit))
                limit = Int32.Parse(parms.Limit);
            if (!String.IsNullOrEmpty(parms.Skip))
            {
                ofs += Int32.Parse(parms.Skip);
                tr = ofs + 1;
            }

            // Timer
            bool exitDoodad = false;
            Task<Action> doodadTask = new Task<Action>(() =>
            {
                int c = 0;
                char[] b = { '|', '/', '-', '\\' };
                while (!exitDoodad)
                {
                    var ips = (((double)(DateTime.Now - start).Ticks / calc) * (tq - calc));
                    var tps = (((double)(DateTime.Now - start).Ticks / calc) * (tr - calc));
                    Console.CursorLeft = 0;
                    Console.Write(new string(' ', Console.WindowWidth - 1));
                    Console.CursorLeft = 0;
                    Console.Write("{8} Calculating care plan {6:0.##} R/S - {0}/{1} <<Scan: {4} ({5:0%})>> ({2:0%}) [ETA: {3} .. {7}] ", calc, tq, (float)calc / tq, new TimeSpan((long)ips).ToString("d'd 'h'h 'm'm 's's'"), ofs, (float)ofs / tr, ((double)calc / (double)(DateTime.Now - start).TotalSeconds), new TimeSpan((long)tps).ToString("d'd 'h'h 'm'm 's's'"),
                        b[c++ % b.Length]);
                    Thread.Sleep(2000);
                }
                return null;
            });
            doodadTask.Start();

            // Process missing patients
            if(parms.Missing)
            {
                var missingPatients = warehouseService.AdhocQuery("SELECT DISTINCT pat_id FROM pat_tbl WHERE NOT EXISTS (SELECT 1 FROM oizcp WHERE patient_id = pat_id)");
                parms.PatientId = new StringCollection();
                parms.PatientId.AddRange(missingPatients.Select(o => $"{o.pat_id}").ToArray());
            }

            // Loop until we've read all patients
            var otr = tr;
            while (ofs < tr)
            {
                // Let the pressure die down
                if (tq - calc > 3000 || ofs % 5000 == 0)
                {
                    if (tq - calc > 0)
                        wtp.WaitOne();
                    MemoryCache.Current.Clear();
                    System.GC.Collect();
                }

                IEnumerable<Patient> prodPatients = null;
                if (!String.IsNullOrEmpty(parms.FacilityId))
                {
                    Guid facId = Guid.Parse(parms.FacilityId);
                    prodPatients = patientPersistence.Query(o => o.Relationships.Where(g => g.RelationshipType.Mnemonic == "DedicatedServiceDeliveryLocation").Any(r => r.TargetEntityKey == facId), queryId, ofs, Environment.ProcessorCount * 4, AuthenticationContext.SystemPrincipal, out tr);
                }
                else if (parms.PatientId?.Count > 0)
                {
                    prodPatients = parms.PatientId.OfType<String>().Skip(ofs).Take(Environment.ProcessorCount * 4).Select(o => patientPersistence.Get(new Identifier<Guid>(Guid.Parse(o)), AuthenticationContext.SystemPrincipal, false)).ToArray().OfType<Patient>();
                    tr = parms.PatientId.Count;
                }
                else
                {
                    // New patients directly modified
                    if (!lastRefresh.HasValue)
                        prodPatients = patientPersistence.Query(o => o.StatusConcept.Mnemonic != "OBSOLETE", queryId, ofs, Environment.ProcessorCount * 4, AuthenticationContext.SystemPrincipal, out tr);
                    else
                        // Patients who have had any participation which has modified them
                        prodPatients = patientPersistence.Query(o => o.StatusConcept.Mnemonic != "OBSOLETE" && o.Participations.Any(p => p.Act.ModifiedOn > lastRefresh), queryId, ofs, Environment.ProcessorCount * 4, AuthenticationContext.SystemPrincipal, out tr);

                }

                if (otr != tr)
                {
                    start = DateTime.Now;
                    otr = tr;
                }

                ofs += prodPatients.Count();
                if (ofs == 0) break; // Done 

                if (!lastRefresh.HasValue)
                {
                    var sk = prodPatients.Count();
                    prodPatients = prodPatients.Where(o => !warehousePatients.Any(m => m.patient_id == o.Key));
                    sk = sk - prodPatients.Count();
                    tq += sk;
                    calc += sk;
                }


                foreach (var p in prodPatients.Distinct(new IdentifiedData.EqualityComparer<Patient>()))
                {
                    if (tq++ > limit)
                    {
                        ofs = tr + 1;
                        ApplicationContext.Current.GetService<MemoryQueryPersistenceService>()?.Clear();
                        break;
                    }

                    List<dynamic> batchInsert = new List<dynamic>();

                    wtp.QueueUserWorkItem(state =>
                    {

                        AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);

                        Patient pState = ApplicationContext.Current.GetService<IDataPersistenceService<Patient>>().Get(new Identifier<Guid>((Guid)state), AuthenticationContext.SystemPrincipal, true);

                        List<dynamic> warehousePlan = new List<dynamic>();

                        Interlocked.Increment(ref calc);

                        var data = pState; //  ApplicationContext.Current.GetService<IDataPersistenceService<Patient>>().Get(p.Key.Value);
                        //p.par
                        // First, we want a copy of the warehouse
                        warehouseService.Delete(dataMart.Id, new
                        {
                            patient_id = data.Key.Value
                        });
                        //warehouseService.Delete(dataMart.Id, new { patient_id = data.Key.Value });
                        var careplanService = ApplicationContext.Current.GetService<ICarePlanService>();

                        // Now calculate the care plan... 
                        var carePlan = careplanService.CreateCarePlan(data, false, new Dictionary<String, Object>() { { "isBackground", true } });
                        warehousePlan.AddRange(carePlan.Action.Select(o => new
                        {
                            creation_date = DateTime.Now,
                            patient_id = data.Key.Value,
                            location_id = data.Relationships.FirstOrDefault(r => r.RelationshipTypeKey == EntityRelationshipTypeKeys.DedicatedServiceDeliveryLocation || r.RelationshipType?.Mnemonic == "DedicatedServiceDeliveryLocation")?.TargetEntityKey.Value,
                            act_id = o.Key.Value,
                            class_id = o.ClassConceptKey.Value,
                            type_id = o.TypeConceptKey.Value,
                            protocol_id = o.Protocols.FirstOrDefault()?.ProtocolKey,
                            min_date = o.StartTime?.DateTime.Date,
                            max_date = o.StopTime?.DateTime.Date,
                            act_date = o.ActTime.DateTime.Date,
                            product_id = o.Participations?.FirstOrDefault(r => r.ParticipationRoleKey == ActParticipationKey.Product || r.ParticipationRole?.Mnemonic == "Product")?.PlayerEntityKey.Value,
                            sequence_id = o.Protocols.FirstOrDefault()?.Sequence,
                            dose_seq = (o as SubstanceAdministration)?.SequenceId,
                            fulfilled = false
                        }));

                        // Insert plans
                        warehouseService.Add(dataMart.Id, warehousePlan);

                    }, p.Key);
                }
            }

            wtp.WaitOne();

            exitDoodad = true;

            return 0;
        }

    }
}
