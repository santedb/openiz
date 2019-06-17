using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace OpenIZ.Core.Model.Subscription
{

    /// <summary>
    /// The subscription modes in which a filter definition applies
    /// </summary>
    [XmlType(nameof(SubscriptionModeType), Namespace = "http://openiz.org/subscription")]
    public enum SubscriptionModeType
    {
        /// <summary>
        /// Only visible when subscription mode is selected
        /// </summary>
        [XmlEnum("subscription")]
        Subscription = 1,
        /// <summary>
        /// Only visible when all data (no specific item) is selected
        /// </summary>
        [XmlEnum("all")]
        All = 2,
        /// <summary>
        /// Always visible
        /// </summary>
        [XmlEnum("*")]
        AllOrSubscription = 3
    }


}
