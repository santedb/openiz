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
 * Date: 2016-7-18
 */
using OpenIZ.Core.Model.Attributes;
using OpenIZ.Core.Model.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Linq;
using OpenIZ.Core.Model.EntityLoader;
using Newtonsoft.Json;
using OpenIZ.Core.Model.Constants;

namespace OpenIZ.Core.Model.Entities
{
    /// <summary>
    /// Represents a name for an entity
    /// </summary>
    [Classifier(nameof(NameUse))]
    [XmlType("EntityName",  Namespace = "http://openiz.org/model"), JsonObject("EntityName")]
    public class EntityName : VersionedAssociation<Entity>
    {
        // Name use key
        private Guid? m_nameUseKey;
        // Name use concept
        
        private Concept m_nameUseConcept;

        /// <summary>
        /// Creates a new name
        /// </summary>
        public EntityName(Guid nameUse, String family, params String[] given)
        {
            this.m_nameUseKey = nameUse;
            this.Component = new List<EntityNameComponent>();

            if (!String.IsNullOrEmpty(family))
                this.Component.Add(new EntityNameComponent(NameComponentKeys.Family, family));
            foreach (var nm in given)
                this.Component.Add(new EntityNameComponent(NameComponentKeys.Given, nm));
        }

        /// <summary>
        /// Creates a new simple name
        /// </summary>
        /// <param name="nameUse"></param>
        /// <param name="name"></param>
        public EntityName(Guid nameUse, String name)
        {
            this.m_nameUseKey = nameUse;
            this.Component = new List<EntityNameComponent>()
            {
                new EntityNameComponent(name)
            };
        }

        /// <summary>
        /// Default ctor
        /// </summary>
        public EntityName()
        {
            this.Component = new List<EntityNameComponent>();
        }

        /// <summary>
        /// Gets or sets the name use key
        /// </summary>
        [XmlElement("use"), JsonProperty("use")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Guid? NameUseKey
        {
            get { return this.m_nameUseKey; }
            set
            {
                if (this.m_nameUseKey != value)
                {
                    this.m_nameUseKey = value;
                    this.m_nameUseConcept = null;
                }
            }
        }

        /// <summary>
        /// Gets or sets the name use
        /// </summary>
        [SerializationReference(nameof(NameUseKey))]
        [XmlIgnore, JsonIgnore, AutoLoad]
        public Concept NameUse
        {
            get {
                this.m_nameUseConcept = base.DelayLoad(this.m_nameUseKey, this.m_nameUseConcept);
                return this.m_nameUseConcept;
            }
            set
            {
                this.m_nameUseConcept = value;
                this.m_nameUseKey = value?.Key;
            }
        }

        /// <summary>
        /// Gets or sets the component types
        /// </summary>
        
        [XmlElement("component"), JsonProperty("component")]
        [AutoLoad]
        public List<EntityNameComponent> Component { get; set; }

        /// <summary>
        /// Refreshes the underlying content
        /// </summary>
        public override void Refresh()
        {
            base.Refresh();
            this.m_nameUseKey = null;
        }

        /// <summary>
        /// Clean object
        /// </summary>
        public override IdentifiedData Clean()
        {
            this.Component.RemoveAll(o => String.IsNullOrEmpty(o.Value));
            return this;

        }


        /// <summary>
        /// True if empty
        /// </summary>
        /// <returns></returns>
        public override bool IsEmpty()
        {
            return this.Component.Count == 0;
        }

    }
}