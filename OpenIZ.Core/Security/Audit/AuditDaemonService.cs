﻿using MARC.HI.EHRS.SVC.Auditing.Data;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.Services.Security;
using OpenIZ.Core.Diagnostics;
using OpenIZ.Core.Interfaces;
using OpenIZ.Core.Model;
using OpenIZ.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Core.Security.Audit
{
    /// <summary>
    /// A daemon service which listens to audit sources and forwards them to the auditor
    /// </summary>
    public class AuditDaemonService : IDaemonService
    {
        private bool m_safeToStop = false;

        // Tracer class
        private TraceSource m_tracer = new TraceSource(OpenIzConstants.SecurityTraceSourceName);

        /// <summary>
        ///  True if the service is running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return true;
            }
        }

        public event EventHandler Started;
        public event EventHandler Starting;
        public event EventHandler Stopped;
        public event EventHandler Stopping;
        
        /// <summary>
        /// Start auditor service
        /// </summary>
        public bool Start()
        {
            this.Starting?.Invoke(this, EventArgs.Empty);

            this.m_safeToStop = false;
            ApplicationContext.Current.Started += (o, e) =>
            {
                try
                {
                    this.m_tracer.TraceInfo("Binding to service events...");

                    ApplicationContext.Current.GetService<IIdentityProviderService>().Authenticated += (so, se) =>
                    {
                        AuditUtil.AuditLogin(se.Principal, se.UserName, so as IIdentityProviderService, se.Success);
                    };
                   

                    // Scan for IRepositoryServices and bind to their events as well
                    foreach (var svc in ApplicationContext.Current.GetServices().OfType<IAuditEventSource>())
                    {
                        svc.DataCreated += (so, se) => AuditUtil.AuditDataAction(EventTypeCodes.PatientRecord, ActionType.Create, AuditableObjectLifecycle.Creation, EventIdentifierType.PatientRecord, se.Success ? OutcomeIndicator.Success : OutcomeIndicator.SeriousFail, null, se.Objects.OfType<IdentifiedData>().ToArray());
                        svc.DataUpdated += (so, se) => AuditUtil.AuditDataAction(EventTypeCodes.PatientRecord, ActionType.Update, AuditableObjectLifecycle.Amendment, EventIdentifierType.PatientRecord, se.Success ? OutcomeIndicator.Success : OutcomeIndicator.SeriousFail, null, se.Objects.OfType<IdentifiedData>().ToArray());
                        svc.DataObsoleted += (so, se) => AuditUtil.AuditDataAction(EventTypeCodes.PatientRecord, ActionType.Delete, AuditableObjectLifecycle.LogicalDeletion, EventIdentifierType.PatientRecord, se.Success ? OutcomeIndicator.Success : OutcomeIndicator.SeriousFail, null, se.Objects.OfType<IdentifiedData>().ToArray());
                        svc.DataDisclosed += (so, se) => AuditUtil.AuditDataAction(EventTypeCodes.Query, ActionType.Read, AuditableObjectLifecycle.Disclosure, EventIdentifierType.Query, se.Success ? OutcomeIndicator.Success : OutcomeIndicator.SeriousFail, se.Query, se.Objects.OfType<IdentifiedData>().ToArray());

                        if (svc is ISecurityAuditEventSource)
                            (svc as ISecurityAuditEventSource).SecurityAttributesChanged += (so, se) => AuditUtil.AuditSecurityAttributeAction(se.Objects, se.Success, se.ChangedProperties);
                    }

                    AuditUtil.AuditApplicationStartStop(EventTypeCodes.ApplicationStart);
                }
                catch (Exception ex)
                {
                    this.m_tracer.TraceError("Error starting up audit repository service: {0}", ex);
                }
            };
            ApplicationContext.Current.Stopping += (o, e) => this.m_safeToStop = true;

            this.Started?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Stopped 
        /// </summary>
        public bool Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);

            // Audit tool should never stop!!!!!
            if (!this.m_safeToStop)
            {
                AuditData securityAlertData = new AuditData(DateTime.Now, ActionType.Execute, OutcomeIndicator.EpicFail, EventIdentifierType.SecurityAlert, AuditUtil.CreateAuditActionCode(EventTypeCodes.UseOfARestrictedFunction));
                AuditUtil.AddDeviceActor(securityAlertData);
                AuditUtil.SendAudit(securityAlertData);
            }

            this.Stopped?.Invoke(this, EventArgs.Empty);
            return true;
        }
    }
}
