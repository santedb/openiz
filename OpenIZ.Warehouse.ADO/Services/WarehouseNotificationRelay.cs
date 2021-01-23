using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using OpenIZ.Core.Diagnostics;
using OpenIZ.Core.Notifications;
using OpenIZ.Warehouse.ADO.Configuration;
using OpenIZ.Warehouse.ADO.Data.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Warehouse.ADO.Services
{
    /// <summary>
    /// A notification relay which uses the Warehouse's message queue
    /// </summary>
    public class WarehouseNotificationRelay : INotificationRelay
    {
        // Configuration
        private AdoConfiguration m_configuration = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection(DataWarehouseConstants.ConfigurationSectionName) as AdoConfiguration;
        
        // Tracer
        private TraceSource m_tracer = new TraceSource(DataWarehouseConstants.TraceSourceName);

        /// <summary>
        /// Gets the scheme of the relay
        /// </summary>
        public string Scheme => "tel";

        /// <summary>
        /// Send to the specified address
        /// </summary>
        public Guid Send(string toAddress, string subject, string body, DateTime? scheduleDelivery = null, IDictionary<String, String> attachments = null)
        {
            var sendUri = new Uri(toAddress);

            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {

                try
                {
                    context.Open();
                    context.BeginTransaction();

                    // Send the message entry
                    var inserted = context.Insert(new MessageQueueEntry()
                    {
                        MessageKey = Guid.NewGuid(),
                        CreationTime = DateTime.Now,
                        Body = body,
                        ScheduledTime = scheduleDelivery,
                        Subject = subject,
                        ToAddress = sendUri.LocalPath
                    });

                    context.Transaction.Commit();
                    return inserted.MessageKey;
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error dispatching message to {0} - {1} on queue table: {2}", toAddress, subject, e);
                    context.Transaction.Rollback();
                    throw;
                }
            }
        }
    }
}
