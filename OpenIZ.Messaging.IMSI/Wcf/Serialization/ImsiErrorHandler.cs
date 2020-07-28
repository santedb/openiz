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
using MARC.HI.EHRS.SVC.Core.Exceptions;
using OpenIZ.Core.Diagnostics;
using OpenIZ.Core.Exceptions;
using OpenIZ.Core.Model.Security;
using OpenIZ.Core.Security.Audit;
using OpenIZ.Core.Wcf.Serialization;
using OpenIZ.Messaging.IMSI.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Diagnostics;
using System.IdentityModel.Tokens;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Authentication;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace OpenIZ.Messaging.IMSI.Wcf.Serialization
{
    /// <summary>
    /// Error handler
    /// </summary>
    public class ImsiErrorHandler : IErrorHandler
    {
        // Trace source
        private TraceSource m_traceSource = new TraceSource("OpenIZ.Messaging.IMSI.Wcf");

        /// <summary>
        /// Handle error
        /// </summary>
        public bool HandleError(Exception error)
        {
            return true;
        }

        /// <summary>
        /// Provide fault
        /// </summary>
        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {

            var uriMatched = WebOperationContext.Current.IncomingRequest.UriTemplateMatch.RequestUri;

            while (error.InnerException != null)
                error = error.InnerException;

            var faultMessage = WebOperationContext.Current.OutgoingResponse;

            // Formulate appropriate response
            if (error is DomainStateException)
                faultMessage.StatusCode = System.Net.HttpStatusCode.ServiceUnavailable;
            else if (error is PolicyViolationException)
            {
                var pve = error as PolicyViolationException;
                if (pve.PolicyDecision == MARC.HI.EHRS.SVC.Core.Services.Policy.PolicyDecisionOutcomeType.Elevate)
                {
                    // Ask the user to elevate themselves
                    faultMessage.StatusCode = HttpStatusCode.Unauthorized;
                }
                else
                {
                    faultMessage.StatusCode = HttpStatusCode.Forbidden;
                }
            }
            else if (error is SecurityException)
            {
                faultMessage.StatusCode = HttpStatusCode.Forbidden;
            }
            else if (error is SecurityTokenException)
            {
                // TODO: Audit this
                faultMessage.StatusCode = System.Net.HttpStatusCode.Unauthorized;
            }
            else if (error is LimitExceededException)
            {
                faultMessage.StatusCode = (HttpStatusCode)429;
                faultMessage.StatusDescription = "Too Many Requests";
                faultMessage.Headers.Add("Retry-After", "1200");
            }
            else if (error is AuthenticationException)
            {
                faultMessage.StatusCode = System.Net.HttpStatusCode.Unauthorized;
            }
            else if (error is UnauthorizedAccessException)
            {
                faultMessage.StatusCode = System.Net.HttpStatusCode.Forbidden;
            }
            else if (error is FaultException)
                faultMessage.StatusCode = HttpStatusCode.InternalServerError;
            else if (error is Newtonsoft.Json.JsonException ||
                error is System.Xml.XmlException)
                faultMessage.StatusCode = System.Net.HttpStatusCode.BadRequest;
            else if (error is DuplicateKeyException || error is DuplicateNameException)
                faultMessage.StatusCode = System.Net.HttpStatusCode.Conflict;
            else if (error is FileNotFoundException || error is KeyNotFoundException)
                faultMessage.StatusCode = System.Net.HttpStatusCode.NotFound;
            else if (error is DomainStateException)
                faultMessage.StatusCode = System.Net.HttpStatusCode.ServiceUnavailable;
            else if (error is DetectedIssueException)
                faultMessage.StatusCode = (System.Net.HttpStatusCode)422;
            else if (error is NotImplementedException)
                faultMessage.StatusCode = HttpStatusCode.NotImplemented;
            else if (error is NotSupportedException)
                faultMessage.StatusCode = HttpStatusCode.MethodNotAllowed;
            else if (error is PatchException)
                faultMessage.StatusCode = HttpStatusCode.Conflict;
            else
                faultMessage.StatusCode = System.Net.HttpStatusCode.InternalServerError;

            switch ((int)faultMessage.StatusCode)
            {
                case 409:
                case 429:
                case 503:
                    this.m_traceSource.TraceInfo("Issue on REST pipeline: {0}", error);
                    break;
                case 401:
                case 403:
                case 501:
                case 405:
                    this.m_traceSource.TraceWarning("Warning on REST pipeline: {0}", error);
                    break;
                default:
                    this.m_traceSource.TraceError("Error on REST pipeline: {0}", error);
                    break;
            }

            // Construct an error result
            var retVal = new ErrorResult(error);
            // Return error in XML only at this point
            fault = new WcfMessageDispatchFormatter<IImsiServiceContract>().SerializeReply(version, null, retVal);

        }
    }
}
