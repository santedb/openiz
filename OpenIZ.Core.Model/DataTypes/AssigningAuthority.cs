﻿/*
 * Copyright 2015-2016 Mohawk College of Applied Arts and Technology
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
 * User: justi
 * Date: 2016-7-16
 */
using Newtonsoft.Json;
using OpenIZ.Core.Model.Attributes;
using OpenIZ.Core.Model.EntityLoader;
using OpenIZ.Core.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace OpenIZ.Core.Model.DataTypes
{
    /// <summary>
    /// Represents a model class which is an assigning authority
    /// </summary>
    [Classifier(nameof(DomainName)), KeyLookup(nameof(DomainName))]
    [XmlType(nameof(AssigningAuthority),  Namespace = "http://openiz.org/model"), JsonObject("AssigningAuthority")]
    [XmlRoot(nameof(AssigningAuthority), Namespace = "http://openiz.org/model")]
    public  class AssigningAuthority : BaseEntityData
    {

        /// <summary>
        /// Assigning authority
        /// </summary>
        public AssigningAuthority()
        {

        }

        /// <summary>
        /// Creates a new assigning authority 
        /// </summary>
        public AssigningAuthority(String domainName, String name, String oid)
        {
            this.DomainName = domainName;
            this.Name = name;
            this.Oid = oid;
        }

        // Assigning device id
        private Guid? m_assigningDeviceId;

        // TODO: Change this to SecurityDevice
        private SecurityDevice m_assigningDevice;

        /// <summary>
        /// Gets or sets the name of the assigning authority
        /// </summary>
        [XmlElement("name"), JsonProperty("name")]
        public String Name { get; set; }
        /// <summary>
        /// Gets or sets the domain name of the assigning authority
        /// </summary>
        [XmlElement("domainName"), JsonProperty("domainName")]
        public String DomainName { get; set; }
        /// <summary>
        /// Gets or sets the description of the assigning authority
        /// </summary>
        [XmlElement("description"), JsonProperty("description")]
        public String Description { get; set; }
        /// <summary>
        /// Gets or sets the oid of the assigning authority
        /// </summary>
        [XmlElement("oid"), JsonProperty("oid")]
        public String Oid { get; set; }
        /// <summary>
        /// The URL of the assigning authority
        /// </summary>
        [XmlElement("url"), JsonProperty("url")]
        public String Url { get; set; }

        /// <summary>
        /// Represents scopes to which the authority is bound
        /// </summary>
        [JsonProperty("scope"), XmlElement("scope")]
        public List<Guid> AuthorityScopeXml { get; set; }

        /// <summary>
        /// Assigning device identifier
        /// </summary>
        [XmlElement("assigningDevice"), JsonProperty("assigningDevice")]
        public Guid? AssigningDeviceKey
        {
            get { return this.m_assigningDeviceId; }
            set
            {
                this.m_assigningDeviceId = value;
                this.m_assigningDevice = null;
            }
        }
        
        /// <summary>
        /// Gets or sets the assigning device
        /// </summary>
        [XmlIgnore, JsonIgnore]
        public SecurityDevice AssigningDevice {
            get
            {
                this.m_assigningDevice = base.DelayLoad(this.m_assigningDeviceId, this.m_assigningDevice);
                return this.m_assigningDevice;
            }
            set
            {
                this.m_assigningDevice = value;
                this.m_assigningDeviceId = value?.Key;
            }
        }

        /// <summary>
        /// Gets concept sets to which this concept is a member
        /// </summary>
        [DataIgnore, XmlIgnore, JsonIgnore, SerializationReference(nameof(AuthorityScopeXml))]
        public List<Concept> AuthorityScope
        {
            get
            {
                return this.AuthorityScopeXml?.Select(o => EntitySource.Current.Get<Concept>(o)).ToList();
            }
            set
            {
                this.AuthorityScopeXml = value?.Where(o => o.Key.HasValue).Select(o => o.Key.Value).ToList();
            }
        }

        /// <summary>
        /// Force reloading of delay load properties
        /// </summary>
        public override void Refresh()
        {
            base.Refresh();
            this.m_assigningDevice = null;
        }
    }
}