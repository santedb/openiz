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
 * User: justi
 * Date: 2016-6-14
 */
using MARC.Everest.DataTypes.Primitives;
using MARC.HI.EHRS.SVC.Core;
using OpenIZ.Core.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Core.Security.Claims
{
    /// <summary>
    /// A claim handler which validates the purpose of use claim
    /// </summary>
    public class PurposeOfUseClaimHandler : IClaimTypeHandler
    {

        private TraceSource m_traceSource = new TraceSource(OpenIzConstants.SecurityTraceSourceName);

        /// <summary>
        /// Gets the name of the claim being validated
        /// </summary>
        public string ClaimType
        {
            get
            {
                return OpenIzClaimTypes.XspaPurposeOfUseClaim;
            }
        }

        /// <summary>
        /// Validate the claim being made
        /// </summary>
        public bool Validate(IPrincipal principal, String value)
        {
            IConceptRepositoryService conceptService = ApplicationContext.Current.GetService<IConceptRepositoryService>();

            try
            {

                // TODO: Validate that the "value" comes from the configured POU domain

                return true;
            }
            catch(Exception e)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
                throw;
            }
        }
    }
}
