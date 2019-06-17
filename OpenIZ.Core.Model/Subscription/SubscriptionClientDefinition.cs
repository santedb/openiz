using Newtonsoft.Json;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace OpenIZ.Core.Model.Subscription
{
    /// <summary>
    /// Represents client side definition
    /// </summary>
    [XmlType(nameof(SubscriptionClientDefinition), Namespace = "http://santedb.org/subscription")]
    [JsonObject(nameof(SubscriptionClientDefinition))]
    public class SubscriptionClientDefinition
    {

        /// <summary>
        /// Gets or sets the resource type reference
        /// </summary>
        [XmlAttribute("resource"), JsonProperty("resource")]
        public string Resource { get; set; }

        /// <summary>
        /// Gets or sets the name of the subscription
        /// </summary>
        [XmlAttribute("name"), JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the mode of the subscription
        /// </summary>
        [XmlAttribute("mode"), JsonProperty("mode")]
        public SubscriptionModeType Mode { get; set; }

        /// <summary>
        /// Gets or sets the trigger
        /// </summary>
        [XmlAttribute("trigger"), JsonProperty("trigger")]
        public SubscriptionTriggerType Trigger { get; set; }

        /// <summary>
        /// Gets or sets the ignore modified on (prevents If-Modified-Since from being used)
        /// </summary>
        [XmlAttribute("ignoreModifiedOn"), JsonProperty("ignoreModifiedOn")]
        public bool IgnoreModifiedOn { get; set; }

        /// <summary>
        /// Gets or sets the guards which indicate when this subscription can be activated
        /// </summary>
        [XmlArray("guards"), XmlArrayItem("when"), JsonProperty("guards")]
        public List<string> Guards { get; set; }

        /// <summary>
        /// Gets or sets the filters
        /// </summary>
        [XmlArray("filters"), XmlArrayItem("add"), JsonProperty("filters")]
        public List<string> Filters { get; set; }

    }
}