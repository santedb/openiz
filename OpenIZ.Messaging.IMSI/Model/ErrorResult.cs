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
using MARC.HI.EHRS.SVC.Core.Services.Policy;
using Newtonsoft.Json;
using OpenIZ.Core.Exceptions;
using OpenIZ.Core.Model;
using OpenIZ.Core.Model.Security;
using OpenIZ.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace OpenIZ.Messaging.IMSI.Model
{
    /// <summary>
    /// Identified data
    /// </summary>
    [XmlType(nameof(ErrorResult), Namespace = "http://openiz.org/imsi")]
    [XmlRoot(nameof(ErrorResult), Namespace = "http://openiz.org/imsi")]
    public class ErrorResult : IdentifiedData
    {

        /// <summary>
        /// Gets the date this was modified
        /// </summary>
        public override DateTimeOffset ModifiedOn
        {
            get
            {
                return DateTimeOffset.Now;
            }
        }

        /// <summary>
        /// Represents an error result
        /// </summary>
        public ErrorResult()
        {
            this.Details = new List<ResultDetail>();
        }

        /// <summary>
        /// Constructs from an exception
        /// </summary>
        public ErrorResult(Exception ex)
        {
            this.Type = ex.GetType().Name;
            this.Message = ex.Message;
#if DEBUG
            this.StackTrace = ex.StackTrace;
            this.Details = new List<ResultDetail>() {
                new ResultDetail(DetailType.Error, ex.ToString())
            };
#endif

            if (ex is PolicyViolationException polViolation)
            {
                this.PolicyId = polViolation.PolicyId;
                this.PolicyOutcome = polViolation.PolicyDecision;
            }

            this.Rules = (ex as DetectedIssueException)?.Issues;
            if (ex.InnerException != null)
                this.Cause = new ErrorResult(ex.InnerException);

        }

        /// <summary>
        /// Gets or sets the details of the result
        /// </summary>
        [XmlElement("detail"), JsonProperty("detail")]
        public List<ResultDetail> Details { get; set; }

        /// <summary>
        /// Gets the message of the erorr
        /// </summary>
        [XmlElement("message"), JsonProperty("message")]
        public String Message { get; set; }

        /// <summary>
        /// Caused by 
        /// </summary>
        [XmlElement("cause"), JsonProperty("cause")]
        public ErrorResult Cause { get; set; }

        /// <summary>
        /// Policy ID was violated
        /// </summary>
        [XmlElement("policyId"), JsonProperty("policyId")]
        public String PolicyId { get; set; }

        /// <summary>
        /// Policy ID was violated
        /// </summary>
        [XmlElement("policyOutcome"), JsonProperty("policyOutcome")]
        public PolicyDecisionOutcomeType PolicyOutcome { get; set; }


        /// <summary>
        /// Detail of exception
        /// </summary>
        [XmlElement("stack"), JsonProperty("stack")]
        public String StackTrace { get; set; }

        /// <summary>
        /// Gets or sets the rules
        /// </summary>
        [XmlElement("rule"), JsonProperty("rules")]
        public List<DetectedIssue> Rules { get; set; }

    }
}
