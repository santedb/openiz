using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace OpenIZ.Core.Scheduling
{
    /// <summary>
    /// Represents clinic scheduling information
    /// </summary>
    [JsonObject(nameof(ClinicServiceScheduleInfo))]
    public class ClinicServiceScheduleInfo 
    {

        /// <summary>
        /// Schedule information
        /// </summary>
        public ClinicServiceScheduleInfo()
        {
            this.Schedule = new List<ClinicScheduleInfo>();
        }

        /// <summary>
        /// Gets or sets the service concept key
        /// </summary>
        [JsonProperty("service")]
        public Guid ServiceConceptKey { get; set; }

        /// <summary>
        /// Gets the schedule
        /// </summary>
        [JsonProperty("schedule")]
        public List<ClinicScheduleInfo> Schedule { get; set; }
    }
}