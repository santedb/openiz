using Newtonsoft.Json;
using System.Xml.Serialization;

namespace OpenIZ.Core.Model.Subscription
{
    /// <summary>
    /// Represents a server subscription definition
    /// </summary>
    [XmlType(nameof(SubscriptionServerDefinition), Namespace = "http://openiz.org/subscription")]
    public class SubscriptionServerDefinition
    {

        /// <summary>
        /// Gets or sets the invariant name
        /// </summary>
        [JsonIgnore, XmlAttribute("invariant")]
        public string InvariantName { get; set; }

        /// <summary>
        /// Gets or sets the SQL definition
        /// </summary>
        [XmlText, JsonIgnore]
        public string Definition { get; set; }

    }
}