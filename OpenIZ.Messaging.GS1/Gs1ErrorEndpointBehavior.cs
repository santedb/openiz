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
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace OpenIZ.Messaging.GS1
{
    /// <summary>
    /// GS1 error andpoint
    /// </summary>
    public class Gs1ErrorEndpointBehavior : WebHttpBehavior
    {
        /// <summary>
        /// Error handlers
        /// </summary>
        protected override void AddServerErrorHandlers(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            base.AddServerErrorHandlers(endpoint, endpointDispatcher);

            //Remove all other error handlers
            endpointDispatcher.ChannelDispatcher.ErrorHandlers.Clear();
            //Add our own
            endpointDispatcher.ChannelDispatcher.ErrorHandlers.Add(new Gs1ErrorHandler());

        }
    }
}