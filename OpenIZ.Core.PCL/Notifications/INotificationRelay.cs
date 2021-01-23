using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Core.Notifications
{

    /// <summary>
    /// A particular relay of messages (e-mail, SMS, etc.) 
    /// </summary>
    public interface INotificationRelay
    {
        /// <summary>
        /// Gets the telecommunications scheme that this relay can handle
        /// </summary>
        String Scheme { get; }

        /// <summary>
        /// Send the specified notification to the specified address
        /// </summary>
        /// <param name="toAddress">The address where the notification is to be sent</param>
        /// <param name="subject">The subject of the message</param>
        /// <param name="body">The body of the message</param>
        /// <param name="scheduleDelivery">The time when the message should be sent (for future delivery)</param>
        /// <param name="attachments">Attachment file and content</param>
        Guid Send(String toAddress, String subject, String body, DateTime? scheduleDelivery = null, IDictionary<String, String> attachments = null);

    }
}
