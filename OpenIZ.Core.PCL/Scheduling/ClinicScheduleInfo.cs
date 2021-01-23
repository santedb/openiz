using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OpenIZ.Core.Scheduling
{
    /// <summary>
    /// Represents a single entry in the clinic schedule
    /// </summary>
    [JsonObject(nameof(ClinicScheduleInfo))]
    public class ClinicScheduleInfo
    {

        /// <summary>
        /// Time parse
        /// </summary>
        private static readonly Regex timeParse = new Regex(@"(\d{2}):(\d{2})");

        /// <summary>
        /// Gets or sets the days of validity
        /// </summary>
        [JsonProperty("days")]
        public List<DayOfWeek> Days { get; set; }

        /// <summary>
        /// Start time
        /// </summary>
        [JsonProperty("start")]
        public String StartXml {
            get => this.Start?.ToString("HH:mm");
            set {
                var timeMatch = timeParse.Match(value);
                if (!timeMatch.Success)
                    throw new FormatException("Time must be HH:mm");
                else
                    this.Start = new DateTime(0001, 01, 01, Int32.Parse(timeMatch.Groups[1].Value), Int32.Parse(timeMatch.Groups[2].Value), 0);
            }
        }

        /// <summary>
        /// Stop time
        /// </summary>
        [JsonProperty("stop")]
        public String StopXml {
            get => this.Stop?.ToString("HH:mm");
            set
            {
                var timeMatch = timeParse.Match(value);
                if (!timeMatch.Success)
                    throw new FormatException("Time must be HH:mm");
                else
                    this.Stop = new DateTime(0001, 01, 01, Int32.Parse(timeMatch.Groups[1].Value), Int32.Parse(timeMatch.Groups[2].Value), 0);
            }
        }

        /// <summary>
        /// Start time
        /// </summary>
        [JsonIgnore]
        public DateTime? Start { get; set; }

        /// <summary>
        /// Stop time
        /// </summary>
        [JsonIgnore]
        public DateTime? Stop { get; set; }

        /// <summary>
        /// Gets or sets the capacity
        /// </summary>
        [JsonProperty("capacity")]
        public int Capacity { get; set; }
    }
}