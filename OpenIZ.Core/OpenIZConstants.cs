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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Core
{
    /// <summary>
    /// OpenIZ constants
    /// </summary>
    public static class OpenIzConstants
    {

        // Configuration name
        public const string OpenIZConfigurationName = "openiz.core";

        // Security trace source
        public const string SecurityTraceSourceName = "OpenIZ.Core.Security";

        // Map trace source
        public const string MapTraceSourceName= "OpenIZ.Core.Map";

        /// <summary>
        /// OpenIZ dataset installation source name
        /// </summary>
        public const string DatasetInstallSourceName = "OpenIZ.Core.DataSet";

        // Client claim header
        public const string BasicHttpClientClaimHeaderName = "X-OpenIZClient-Claim";
        // Client auth header
        public const string BasicHttpClientCredentialHeaderName = "X-OpenIZClient-Authorization";
        // WCF trace source
        public const string WcfTraceSourceName = "OpenIZ.Core.Wcf";

        // Panic error code
        public const string GeneralPanicErrorCode = "01189998819991197253";
        // General panic error text
        public const string GeneralPanicErrorText = "0118 999 881 999 119 7253 - FATAL ERROR: {0}";

        /// <summary>
        /// Service trace source name
        /// </summary>
        public const string ServiceTraceSourceName = "OpenIZ.Core";
    }
}
