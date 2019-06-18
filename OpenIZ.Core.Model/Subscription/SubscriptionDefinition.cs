using Newtonsoft.Json;
using OpenIZ.Core.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace OpenIZ.Core.Model.Subscription
{
    /// <summary>
    /// Class which is used to define a subscription type which clients can consume
    /// </summary>
    [XmlType(nameof(SubscriptionDefinition), Namespace = "http://openiz.org/subscription")]
    [XmlRoot(nameof(SubscriptionDefinition), Namespace = "http://openiz.org/subscription")]
    [JsonObject(nameof(SubscriptionDefinition))]
    public class SubscriptionDefinition : IdentifiedData
    {

        // True if server definitions should be included
        private bool m_includeServerDefs = true;

        /// <summary>
        /// Gets or sets the uuid
        /// </summary>
        [XmlAttribute("uuid"), JsonProperty("uuid")]
        public Guid Uuid { get => base.Key.GetValueOrDefault(); set => base.Key = value; }

        /// <summary>
        /// Don't include uuid
        /// </summary>
        public bool ShouldSerializeUuid() => false;

        /// <summary>
        /// Default ctor
        /// </summary>
        public SubscriptionDefinition()
        {
            this.Key = Guid.NewGuid();
            this.ServerDefinitions = new List<SubscriptionServerDefinition>();
            this.ClientDefinitions = new List<SubscriptionClientDefinition>();
        }


        /// <summary>
        /// Get locked copy of the object for sending to clients
        /// </summary>
        public override IdentifiedData GetLocked()
        {

            var retVal = base.GetLocked();
            (retVal as SubscriptionDefinition).m_includeServerDefs = false;
            return retVal;
        }

        /// <summary>
        /// Gets the time that this was modified
        /// </summary>
        public override DateTimeOffset ModifiedOn => DateTime.Now;

        /// <summary>
        /// Gets or sets the resource type
        /// </summary>
        [XmlAttribute("resource"), JsonIgnore]
        public String Resource { get; set; }

        /// <summary>
        /// Gets the resource type
        /// </summary>
        [XmlIgnore, JsonIgnore]
        public Type ResourceType => new ModelSerializationBinder().BindToType("OpenIZ.Core.Model", this.Resource);

        /// <summary>
        /// Gets or sets the server definitions
        /// </summary>
        [XmlArray("server"), XmlArrayItem("definition"), JsonIgnore]
        public List<SubscriptionServerDefinition> ServerDefinitions { get; set; }

        /// <summary>
        /// Should serialize
        /// </summary>
        public bool ShouldSerializeServerDefinitions() => this.m_includeServerDefs;

        /// <summary>
        /// Gets or sets the client side definitions
        /// </summary>
        [XmlArray("client"), XmlArrayItem("definition"), JsonProperty("definitions")]
        public List<SubscriptionClientDefinition> ClientDefinitions { get; set; }

        /// <summary>
        /// Load the specified subscription definition 
        /// </summary>
        public static SubscriptionDefinition Load(MemoryStream ms)
        {
            return new XmlSerializer(typeof(SubscriptionDefinition)).Deserialize(ms) as SubscriptionDefinition;
        }
    }
}
