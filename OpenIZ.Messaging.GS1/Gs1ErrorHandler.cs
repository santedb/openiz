/*
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
using OpenIZ.Core.Wcf.Serialization;
using OpenIZ.Messaging.IMSI.Model;
using OpenIZ.Messaging.IMSI.Wcf;
using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace OpenIZ.Messaging.GS1
{
    /// <summary>
    /// Error handler for GS1 endpoint
    /// </summary>
    public class Gs1ErrorHandler : WcfErrorHandler
    {

        /// <summary>
        /// Provide fault
        /// </summary>
        public override void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
            base.ProvideFault(error, version, ref fault);

            // Construct an error result
            var retVal = new ErrorResult(error);
            
            // Return error in XML only at this point
            fault = new WcfMessageDispatchFormatter<IImsiServiceContract>().SerializeReply(version, null, retVal);
        }
    }
}