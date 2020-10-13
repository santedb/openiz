using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace OpenIZ.Core.Security.Notification
{
    /// <summary>
    /// Notification template
    /// </summary>
    [XmlType(nameof(NotificationTemplate), Namespace = "http://openiz.org/notification")]
    [XmlRoot(nameof(NotificationTemplate), Namespace = "http://openiz.org/notification")]
    public class NotificationTemplate 
    {

        // Serializer
        private static XmlSerializer s_xsz = new XmlSerializer(typeof(NotificationTemplate));

        /// <summary>
        /// Gets or sets the subject
        /// </summary>
        [XmlElement("subject")]
        public String SubjectLine { get; set; }

        /// <summary>
        /// Gets or sets the body 
        /// </summary>
        [XmlElement("body")]
        public String BodyText { get; set; }


        /// <summary>
        /// Load the specified file
        /// </summary>
        public static NotificationTemplate Load(Stream s)
        {
            return s_xsz.Deserialize(s) as NotificationTemplate;
        }
    }
}
