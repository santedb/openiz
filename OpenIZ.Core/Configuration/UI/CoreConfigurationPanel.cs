﻿/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: fyfej
 * Date: 2017-9-1
 */
using MARC.HI.EHRS.SVC.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace OpenIZ.Core.Configuration.UI
{
    /// <summary>
    /// Core Configuration Panel for OpenIZ
    /// </summary>
    public class CoreConfigurationPanel : IConfigurableFeature
    {
        // Configuration panel
        private ucCoreSettings m_panel = new ucCoreSettings();

        /// <summary>
        /// Always configure this feature
        /// </summary>
        public bool AlwaysConfigure
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Enable the configuration
        /// </summary>
        public bool EnableConfiguration
        {
            get;set;
        }

        /// <summary>
        /// Get or set the name
        /// </summary>
        public string Name
        {
            get
            {
                return "OpenIZ/Options";
            }
        }

        /// <summary>
        /// Gets the panel
        /// </summary>
        public Control Panel
        {
            get
            {
                return this.m_panel;
            }
        }

        /// <summary>
        /// Configure the features
        /// </summary>
        public void Configure(XmlDocument configurationDom)
        {
            // Configure the options

            // Section
            var sectionDefinition = configurationDom.GetOrAddSectionDefinition(OpenIzConstants.OpenIZConfigurationName, typeof(ConfigurationSectionHandler));
            var section = configurationDom.GetSectionXml(OpenIzConstants.OpenIZConfigurationName);
            var config = this.m_panel.Configuration;

            // First thread pooling
            section.UpdateOrCreateChildElement("threading", new { poolSize = config.ThreadPoolSize });

            // Now security section
            var securityElement = section.GetOrCreateElement("security");
            var appletElement = section.UpdateOrCreateChildElement("applet", new { allowUnsignedApplets = config.Security.AllowUnsignedApplets });

            // Trusted publishers
            var trustedPublishers = appletElement.GetOrCreateElement("trustedPublishers");
            trustedPublishers.RemoveAll();
            foreach (var tp in config.Security.TrustedPublishers)
                trustedPublishers.AppendChild(configurationDom.CreateElementValue("add", tp));

            // Create audiences from the configured WCF interfaces
            foreach(var wcfIface in configurationDom.SelectNodes("//service/endpoint[@binding = 'webHttpBinding']"))
            {
                // Get binding

            }
        }

        /// <summary>
        /// Easy configure the features
        /// </summary>
        /// <param name="configFile"></param>
        public void EasyConfigure(XmlDocument configFile)
        {
        }

        /// <summary>
        /// True if the service is configured
        /// </summary>
        public bool IsConfigured(XmlDocument configurationDom)
        {
            var config = configurationDom.GetSection("openiz.core") as OpenIzConfiguration;
            this.m_panel.Configuration = config;
            return config != null;
        }

        /// <summary>
        /// Un-configure the option
        /// </summary>
        public void UnConfigure(XmlDocument configurationDom)
        {
        }

        /// <summary>
        /// Validate the configuration
        /// </summary>
        public bool Validate(XmlDocument configurationDom)
        {
            return true;
        }
    }
}
