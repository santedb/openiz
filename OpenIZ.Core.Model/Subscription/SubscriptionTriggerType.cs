using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace OpenIZ.Core.Model.Subscription
{

    /// <summary>
    /// Represents synchronization pull triggers
    /// </summary>
    [XmlType(nameof(SubscriptionTriggerType), Namespace = "http://openiz.org/subscription")]
    [Flags]
    public enum SubscriptionTriggerType
    {
        /// <summary>
        /// Never execute the trigger
        /// </summary>
        [XmlEnum("never")]
        Never = 0x0,
        /// <summary>
        /// Always execute the trigger
        /// </summary>
        [XmlEnum("always")]
        Always = OnStart | OnCommit | OnStop | OnPush | OnNetworkChange | PeriodicPoll,
        /// <summary>
        /// Only on start
        /// </summary>
        [XmlEnum("on-start")]
        OnStart = 0x01,
        /// <summary>
        /// Only on commit
        /// </summary>
        [XmlEnum("on-commit")]
        OnCommit = 0x02,
        /// <summary>
        /// Only on stop
        /// </summary>
        [XmlEnum("on-stop")]
        OnStop = 0x04,
        /// <summary>
        /// Only on push of data
        /// </summary>
        [XmlEnum("on-push")]
        OnPush = 0x08,
        /// <summary>
        /// Only when the network changes
        /// </summary>
        [XmlEnum("on-x-net")]
        OnNetworkChange = 0x10,
        /// <summary>
        /// Periodically poll
        /// </summary>
        [XmlEnum("periodic")]
        PeriodicPoll = 0x20,
        /// <summary>
        /// Only when manually pulling
        /// </summary>
        [XmlEnum("manual")]
        Manual = 0x40
    }
}
