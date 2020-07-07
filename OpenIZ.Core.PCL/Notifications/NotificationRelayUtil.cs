using OpenIZ.Core.Interfaces;
using OpenIZ.Core.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Core.Notifications
{
    /// <summary>
    /// Notification relay utilities
    /// </summary>
    public static class NotificationRelayUtil
    {


        // Relay cache
        private static IDictionary<String, INotificationRelay> m_relays;

        /// <summary>
        /// Gets the notification relay for the telecom address
        /// </summary>
        public static INotificationRelay GetNotificationRelay(this EntityTelecomAddress me)
        {
            return GetNotificationRelay(me.IETFValue);
        }

        /// <summary>
        /// Gets the appropriate relay service for the specified type of address
        /// </summary>
        /// <param name="address">The address where the message should be sent</param>
        /// <returns>The notification relay</returns>
        public static INotificationRelay GetNotificationRelay(String address)
        {

            var uri = new Uri(address);
            if (m_relays == null)
                m_relays = (ApplicationServiceContext.Current.GetService(typeof(IServiceManager)) as IServiceManager).GetAllTypes().Where(o => typeof(INotificationRelay).GetTypeInfo().IsAssignableFrom(o.GetTypeInfo()) && !o.GetTypeInfo().IsInterface).Select(o => Activator.CreateInstance(o) as INotificationRelay).ToDictionary(o => o.Scheme, o => o);
            if (m_relays.TryGetValue(uri.Scheme, out INotificationRelay relay))
                return relay;
            return null;

        }
    }
}
