using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using OpenIZ.Core.Notifications;
using OpenIZ.Persistence.Diagnostics.Email.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Persistence.Diagnostics.Email
{
    /// <summary>
    /// Notification relay
    /// </summary>
    /// <remarks>Should be moved to its own assembly</remarks>
    public class EMailNotificationRelay : INotificationRelay
    {
        // Trace source
        private TraceSource m_traceSource = new TraceSource("OpenIZ.Persistence.Diagnostics.Email");

        // Configuration
        private DiagnosticEmailServiceConfiguration m_configuration = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection("openiz.persistence.diagnostics.email") as DiagnosticEmailServiceConfiguration;

        /// <summary>
        /// Get the scheme
        /// </summary>
        public string Scheme => "mailto";

        /// <summary>
        /// Send the specified relay information
        /// </summary>
        public Guid Send(string toAddress, string subject, string body, DateTime? scheduleDelivery = null, IDictionary<string, string> attachments = null)
        {
            try
            {
                // Setup message
                var toUri = new Uri(toAddress);
                MailMessage bugMessage = new MailMessage();
                bugMessage.From = new MailAddress(this.m_configuration.Smtp.From, $"OpenIZ Notification Service");
                bugMessage.To.Add(toAddress.Replace("mailto:",""));
                bugMessage.Subject = subject;
                bugMessage.Body = body;

                // Add attachments
                foreach (var itm in attachments)
                {
                    bugMessage.Attachments.Add(new Attachment(new MemoryStream(Encoding.UTF8.GetBytes(itm.Value)), itm.Key, "text/plain"));
                }

                SmtpClient smtpClient = new SmtpClient(this.m_configuration.Smtp.Server.Host, this.m_configuration.Smtp.Server.Port);
                smtpClient.UseDefaultCredentials = String.IsNullOrEmpty(this.m_configuration.Smtp.Username);
                smtpClient.EnableSsl = this.m_configuration.Smtp.Ssl;
                if (!(smtpClient.UseDefaultCredentials))
                    smtpClient.Credentials = new NetworkCredential(this.m_configuration.Smtp.Username, this.m_configuration.Smtp.Password);
                smtpClient.SendCompleted += (o, e) =>
                {
                    this.m_traceSource.TraceInformation("Successfully sent message to {0}", bugMessage.To);
                    if (e.Error != null)
                        this.m_traceSource.TraceEvent(TraceEventType.Error, 0, e.Error.ToString());
                    (o as IDisposable).Dispose();
                };
                this.m_traceSource.TraceInformation("Sending notification email message to {0}", bugMessage.To);
                smtpClient.Send(bugMessage);
                return Guid.Empty;
            }
            catch (Exception ex)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Error, ex.HResult, "Error sending notification: {0}", ex);
                throw;
            }
        }
    }
}
