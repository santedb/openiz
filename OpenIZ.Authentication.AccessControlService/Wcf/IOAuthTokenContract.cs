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
using OpenIZ.Authentication.OAuth2.Model;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Authentication.OAuth2.Wcf
{
    /// <summary>
    /// OAuth2.0 Contract
    /// </summary>
    [ServiceContract(ConfigurationName = "OpenIZ.Authentication.OAuth2")]
    public interface IOAuthTokenContract 
    {

        /// <summary>
        /// Token request
        /// </summary>
        [WebInvoke(UriTemplate = "oauth2_token", Method = "POST")]
        [OperationContract]
        Stream Token(Message inboundData);
    }
}
