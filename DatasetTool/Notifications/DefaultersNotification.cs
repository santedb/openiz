using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.Services.Security;
using OpenIZ.Core.Model.Entities;
using OpenIZ.Core.Security;
using OpenIZ.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OizDevTool.Notifications
{
    /// <summary>
    /// Notification for defaulters
    /// </summary>
    public class DefaultersNotification : INotificationProcess
    {
        /// <summary>
        /// Process the specified notification
        /// </summary>
        public void Process(DateTime date, string facilityId, string language)
        {
            try
            {

                // People who were supposed to show up two weeks ago
                DateTime fromDate = date.AddDays(-21),
                    toDate = date.AddDays(-14);

                var warehouseService = ApplicationContext.Current.GetService<IAdHocDatawarehouseService>();
                var placeService = ApplicationContext.Current.GetService<IDataPersistenceService<Place>>() as IStoredQueryDataPersistenceService<Place>;
                var productService = ApplicationContext.Current.GetService<IDataPersistenceService<Material>>() as IStoredQueryDataPersistenceService<Material>;
                var userService = ApplicationContext.Current.GetService<IDataPersistenceService<UserEntity>>();
                var dataMart = warehouseService.GetDatamart("oizcp");
                var roleService = ApplicationContext.Current.GetService<IRoleProviderService>();
                var securityService = ApplicationContext.Current.GetService<ISecurityRepositoryService>();


                Dictionary<Guid, Material> productRefs = new Dictionary<Guid, Material>();
                foreach (var itm in productService.Query(o => o.StatusConcept.Mnemonic == "ACTIVE" && o.TypeConcept.Mnemonic.Contains("VaccineType-*"), AuthenticationContext.SystemPrincipal))
                {
                    if (!productRefs.ContainsKey(itm.Key.Value))
                        productRefs.Add(itm.Key.Value, itm);
                }

                Console.WriteLine("Getting defaulters due between {0:o} - {1:o}", fromDate, toDate);
                var query = @"WITH RECURSIVE facilities(fac_id) AS (
	                SELECT 
		                fac_tbl.fac_id,
		                fac_tbl.parent_id
	                FROM
		                fac_tbl
	                WHERE
					                fac_id = '{0}'::UUID
	                UNION ALL 
	                SELECT 
		                fac_tbl.fac_id,
		                fac_tbl.parent_id
	                FROM
		                facilities INNER JOIN fac_tbl ON(fac_tbl.parent_id = facilities.fac_id)
                )
	                SELECT 
		                location_id, 
		                patient_id,
		                given,
		                alt_id, 
		                FIRST(coalesce(pat_vw.tel, mth_tel, nok_tel)) as tel,
		                array_agg(product_id) as product,
		                array_agg(act_date) as dates, 
                        array_agg(uuid) as cp_id,
		                fac_vw.loc_name
	                FROM
		                oizcp
		                INNER JOIN pat_vw ON (pat_id = patient_id)
		                INNER JOIN fac_vw ON (oizcp.location_id = fac_vw.fac_id)
		                INNER JOIN facilities ON (facilities.fac_id = oizcp.location_id)
	                WHERE
		                act_date::DATE BETWEEN COALESCE('{1}')::DATE AND COALESCE('{2}')::DATE
		                AND act_date::DATE < CURRENT_DATE - '7 DAY'::INTERVAL 
		            	AND NOT EXISTS (SELECT 1 FROM MSG_QUEUE_LOG_TBL WHERE REF_ID = UUID AND PROC_NAME = 'DefaultersNotification')
                        AND NOT EXISTS (SELECT DISTINCT TRUE FROM sbadm_tbl WHERE pat_id = patient_id AND mat_id = product_id AND seq_id = dose_seq AND COALESCE(neg_ind, FALSE) = FALSE  )
		                AND (
			                dose_seq = 1
			                OR
			                EXISTS (
				                SELECT TRUE 
				                FROM sbadm_tbl 
				                WHERE
					                seq_id = dose_seq - 1
					                AND pat_id = patient_id
					                AND mat_id = product_id
					                AND COALESCE(neg_ind, FALSE) = FALSE
                          AND sbadm_tbl.type_mnemonic <> 'DrugTherapy'
                          AND enc_id IS NOT NULL
			                )
		                )
	                GROUP BY location_id, patient_id, given, alt_id, fac_vw.loc_name";

                var defaulters = warehouseService.AdhocQuery(String.Format(query, facilityId ?? "6130e1ce-3ed1-467f-8c33-2f96e47674f7", fromDate, toDate));

                Console.WriteLine("There are {0} defaulters during this time", defaulters.Count());
                foreach (var defaulter in defaulters)
                {
                    if (DBNull.Value.Equals(defaulter.tel))
                    {
                        Console.WriteLine("Skipping for patient with barcode {0} because not telephone is present", defaulter.alt_id);
                        Trace.TraceWarning("Skipping careplan entry {0} for patient with barcode {1} because not telephone is present", String.Join(",", new List<Guid>(defaulter.cp_id)), (string)defaulter.alt_id);
                        continue;
                    }
                    // First pre-req data for this patient
                    var products = new List<Guid>(defaulter.product).Select(o => productRefs[o]);
                    var scheduleDate = new List<DateTime>(defaulter.dates).Max();
                    var templateValues = new Dictionary<string, String>()
                    {
                        { "given", defaulter.given.ToString() },
                        { "product", NotificationUtils.GetProductList(products, language) },
                        { "fac_name", defaulter.loc_name.ToString() },
                        { "date", scheduleDate.Date.ToString("ddd dd MMM, yyyy") }
                    };

                    // Send and record
                    var msgId = NotificationUtils.SendNotification("DefaulterNotification", defaulter.tel, "Defaulter Notification", DateTime.Now.AddDays(1), templateValues);
                    
                    foreach(var itm in new List<Guid>(defaulter.cp_id))
                        warehouseService.AdhocQuery(String.Format("INSERT INTO MSG_QUEUE_LOG_TBL (MSG_ID, PAT_ID, PROC_NAME, REF_ID) VALUES ('{0}', '{1}', '{2}', '{3}')", msgId, defaulter.patient_id, nameof(DefaultersNotification), itm));

                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Could not send defaulters notification: {0}", e);
            }
        }
    }
}
