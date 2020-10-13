using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using OpenIZ.Core;
using OpenIZ.Core.Configuration;
using OpenIZ.Core.Notifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Notification
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
        private OpenIzConfiguration m_configuration = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection(OpenIzConstants.OpenIZConfigurationName) as OpenIzConfiguration;

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
                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(this.m_configuration.Notification.Smtp.From);
                mailMessage.To.Add(toAddress.Replace("mailto:",""));
                mailMessage.Subject = subject;
                mailMessage.Body = body;

                if(body.Contains("<html"))
                    mailMessage.IsBodyHtml = true;
                // Add attachments
                if(attachments != null)
                    foreach (var itm in attachments)
                    {
                        mailMessage.Attachments.Add(new Attachment(new MemoryStream(Encoding.UTF8.GetBytes(itm.Value)), itm.Key, "text/plain"));
                    }

                SmtpClient smtpClient = new SmtpClient(this.m_configuration.Notification.Smtp.Server.Host, this.m_configuration.Notification.Smtp.Server.Port);
                smtpClient.UseDefaultCredentials = String.IsNullOrEmpty(this.m_configuration.Notification.Smtp.Username);
                smtpClient.EnableSsl = this.m_configuration.Notification.Smtp.Ssl;
                if (!(smtpClient.UseDefaultCredentials))
                    smtpClient.Credentials = new NetworkCredential(this.m_configuration.Notification.Smtp.Username, this.m_configuration.Notification.Smtp.Password);
                smtpClient.SendCompleted += (o, e) =>
                {
                    this.m_traceSource.TraceInformation("Successfully sent message to {0}", mailMessage.To);
                    if (e.Error != null)
                        this.m_traceSource.TraceEvent(TraceEventType.Error, 0, e.Error.ToString());
                    (o as IDisposable).Dispose();
                };
                this.m_traceSource.TraceInformation("Sending notification email message to {0}", mailMessage.To);
                smtpClient.Send(mailMessage);
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
