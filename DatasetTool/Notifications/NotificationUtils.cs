using OpenIZ.Core.Model.DataTypes;
using OpenIZ.Core.Model.Entities;
using OpenIZ.Core.Notifications;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OpenIZ.Core.Model;
using OpenIZ.Core.Model.Constants;

namespace OizDevTool.Notifications
{

    /// <summary>
    /// Notification utility
    /// </summary>
    public static class NotificationUtils
    {

        /// <summary>
        /// Send a notification
        /// </summary>
        public static Guid SendNotification(String template, String to, String subject, DateTime scheduleSend, Dictionary<String, String> variableValues)
        {
            var templateData = GetTemplate(template);
            foreach (var kv in variableValues)
                templateData = templateData.Replace($"{{{kv.Key}}}", kv.Value);
            if (!String.IsNullOrEmpty(to))
                return NotificationRelayUtil.GetNotificationRelay($"tel:{to}").Send($"tel:{to}", subject, templateData, scheduleSend);
            return Guid.Empty;
        }

        /// <summary>
        /// Get the specified SMS template
        /// </summary>
        public static string GetTemplate(String name)
        {
            var templateFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), name + ".sms");
            if (File.Exists(templateFile))
                return File.ReadAllText(templateFile);
            throw new KeyNotFoundException($"Could not find template file {name}");
        }

        /// <summary>
        /// Get product list as a string
        /// </summary>
        public static string GetProductList(IEnumerable<Material> products, string language) =>
            String.Join(",", products.Select(product =>
            {
                var names = product.LoadCollection<EntityName>(nameof(Material.Names))
                    .ToArray();
                
                var name = names.FirstOrDefault(o => o.NameUseKey == NameUseKeys.Search)
                    ?? names.FirstOrDefault(o => o.NameUseKey == NameUseKeys.Assigned)
                    ?? names.First();
                return name.LoadCollection<EntityNameComponent>(nameof(EntityName.Component)).First().Value;
            }));
    }
}
