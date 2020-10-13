using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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

        // Binding Regex
        private static Regex m_bindingRegex = new Regex("{{\\s?\\$([A-Za-z0-9_]*?)\\s?}}");

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

        /// <summary>
        /// Parse the fields in <paramref name="instance"/> and create a replacement dictionary
        /// </summary>
        public static IDictionary<String, String> GetTemplateFields(Object instance)
        {
            var retVal = new Dictionary<String, String>();
            foreach (var p in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                retVal.Add(p.Name, p.GetValue(instance, null)?.ToString());
            }
            return retVal;
        }

        /// <summary>
        /// Fill the specified template
        /// </summary>
        public static NotificationTemplate FillTemplate(String templateName, IDictionary<String, String> templateFields)
        {
            NotificationTemplate retVal = null;
            using (var fs = File.OpenRead(Path.ChangeExtension(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "templates", templateName), "xml")))
                retVal = NotificationTemplate.Load(fs);

            // Process headers
            retVal.SubjectLine = ReplaceTemplate(retVal.SubjectLine, templateFields);
            retVal.BodyText = ReplaceTemplate(retVal.BodyText, templateFields);

            return retVal;
        }

        /// <summary>
        /// Replace template
        /// </summary>
        private static string ReplaceTemplate(string source, IDictionary<string, string> templateFields)
        {
            return m_bindingRegex.Replace(source, (m) => templateFields.TryGetValue(m.Groups[1].Value, out string v) ? v : m.ToString());
        }
    }
}
