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
 * User: khannan
 * Date: 2017-9-1
 */

using MARC.HI.EHRS.SVC.Auditing.Services;
using MARC.HI.EHRS.SVC.Core;
using OpenIZ.Core.Diagnostics;
using OpenIZ.Core.Model.AMI.Security;
using OpenIZ.Core.Security;
using OpenIZ.Core.Services;
using System;
using System.Linq;
using System.Security;
using System.ServiceModel.Web;

namespace OpenIZ.Messaging.AMI.Wcf
{
	/// <summary>
	/// AMI Behavior for auditing
	/// </summary>
	public partial class AmiBehavior
	{
		/// <summary>
		/// Create/send an audit
		/// </summary>
		/// <param name="audit">The audit.</param>
		/// <exception cref="System.Security.SecurityException">Audit service does not exist!</exception>
		public void CreateAudit(AuditInfo audit)
		{
			try
			{
				// Audit the access
				var auditService = ApplicationContext.Current.GetService<IAuditorService>();
                if (auditService == null)
                    this.traceSource.TraceWarning("Could not find audit service, no audits will be dispatched to central repository");

				var authContext = AuthenticationContext.Current;
				ApplicationContext.Current.GetService<IThreadPoolService>()?.QueueNonPooledWorkItem(o =>
				{
					try
					{
						var adt = o as AuditInfo;
						AuthenticationContext.Current = authContext;
                        if(auditService != null)
						adt.Audit.ForEach(a => auditService.SendAudit(a));

						// Persist this audit as well?
						var auditRepositoryService = ApplicationContext.Current.GetService<IAuditRepositoryService>();

						if (auditRepositoryService != null)
						{
							adt.Audit = adt.Audit.Select(a => auditRepositoryService.Insert(a)).ToList();
						}
					}
					catch (Exception e)
					{
						this.traceSource.TraceError("Error forwarding/persisting audit: {0}", e);
					}
				}, audit);

			}
			catch (Exception e)
			{
				this.traceSource.TraceError("Error creating audit: {0}", e);
			}
            finally
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NoContent;

            }
        }
	}
}