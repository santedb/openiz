﻿/*
 * Copyright 2016-2016 Mohawk College of Applied Arts and Technology
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
 * Date: 2016-1-24
 */
using Newtonsoft.Json;
using OpenIZ.Core.Model.Attributes;
using OpenIZ.Core.Model.DataTypes;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace OpenIZ.Core.Model.Entities
{
    /// <summary>
    /// A generic class representing components of a larger item (i.e. address, name, etc);
    /// </summary>
    /// <typeparam name="TBoundModel"></typeparam>
    [Classifier(nameof(Type))]
    [XmlType(Namespace = "http://openiz.org/model")]
    public abstract class GenericComponentValues<TBoundModel> : Association<TBoundModel> where TBoundModel : IdentifiedData
    {
        // Component type
        private Guid m_componentTypeKey;
        // Component type
        
        private Concept m_componentType;

        /// <summary>
        /// Component type key
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        
        [XmlElement("type"), JsonProperty("type")]
        public Guid ComponentTypeKey
        {
            get { return this.m_componentTypeKey; }
            set
            {
                this.m_componentTypeKey = value;
                this.m_componentType = null;
            }
        }

        /// <summary>
        /// Gets or sets the type of address component
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [DelayLoad(nameof(ComponentTypeKey))]
        public Concept ComponentType
        {
            get {
                this.m_componentType = base.DelayLoad(this.m_componentTypeKey, this.m_componentType);
                return this.m_componentType;
            }
            set
            {
                this.m_componentType = value;
                if (value == null)
                    this.m_componentTypeKey = Guid.Empty;
                else
                    this.m_componentTypeKey = value.Key;
            }
        }

        /// <summary>
        /// Forces refreshing of delay load properties
        /// </summary>
        public override void Refresh()
        {
            base.Refresh();
            this.m_componentType = null;
        }
    }
}