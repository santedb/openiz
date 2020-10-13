using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Timer;
using OpenIZ.Core.Notifications;
using OpenIZ.Core.Security.Notification;
using OpenIZ.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace OpenIZ.Core.Security.Jobs
{
    /// <summary>
    /// Represents a job that prunes users
    /// </summary>
    public class UserPruneJob : ITimerJob
    {

        // Tracer
        private TraceSource m_tracer = new TraceSource("OpenIZ.Core.Security");

        /// <summary>
        /// The timer JOB has expired
        /// </summary>
        public void Elapsed(object sender, ElapsedEventArgs e)
        {

            try
            {
                this.m_tracer.TraceInformation("Will notify users of inactivity...");
                var secRepo = ApplicationContext.Current.GetService<ISecurityRepositoryService>();
                DateTimeOffset cutoff = DateTimeOffset.Now.AddDays(-28); // TODO: Make this configurable
                var relay = NotificationRelayUtil.GetNotificationRelay($"mailto:");

                int offset = 0, totalResults = 1;
                while(offset < totalResults)
                {
                    // Users who haven't logged in 
                    foreach (var usr in secRepo.FindUsers(o => o.Email.Contains("@") && (o.LastLoginTime < cutoff || o.LastLoginTime == null), offset, 100, out totalResults))
                    {
                        var fields = NotificationTemplate.GetTemplateFields(usr);
                        double days = 0;
                        if (!usr.LastLoginTime.HasValue)
                            days = DateTimeOffset.Now.Subtract(usr.CreationTime).TotalDays;
                        else
                            days = DateTimeOffset.Now.Subtract(usr.LastLoginTime.Value).TotalDays;

                        if(days > 35)
                        {
                            secRepo.ObsoleteUser(usr.Key.Value);
                            var email = NotificationTemplate.FillTemplate("AccountAutoDeleted", fields);
                            relay.Send(usr.Email, email.SubjectLine, email.BodyText);
                        }
                        else if(days > 28)
                        {
                            fields.Add("days", (35 - days).ToString());
                            var email = NotificationTemplate.FillTemplate("AccountInactivityNotify", fields);
                            relay.Send(usr.Email, email.SubjectLine, email.BodyText);
                        }
                    }
                    offset += 100;
                }
            }
            catch(Exception ex)
            {
                this.m_tracer.TraceEvent(TraceEventType.Error, ex.HResult, "Error running user prune job: {0}", ex);
            }

        }
    }
}
