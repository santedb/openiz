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
 * Date: 2016-1-19
 */
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using OpenIZ.Core.Model.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Core.Model.Security
{
    /// <summary>
    /// Security role
    /// </summary>
    [XmlType(Namespace = "http://openiz.org/model", TypeName = "SecurityRole")]
    [Serializable]
    [XmlRoot(Namespace = "http://openiz.org/model", ElementName = "SecurityRole")]
    public class SecurityRole : SecurityEntity
    {

        // User delay load
        
        private List<SecurityUser> m_users;
        
        /// <summary>
        /// Gets or sets the name of the security role
        /// </summary>
        [XmlElement("name")]
        public String Name { get; set; }

        /// <summary>
        /// Description of the role
        /// </summary>
        [XmlElement("description")]
        public String Description { get; set; }

        /// <summary>
        /// Gets or sets the security users in the role
        /// </summary>
        [XmlIgnore]
        [DelayLoad(null)]
        public List<SecurityUser> Users {
            get
            {
                if (this.IsDelayLoadEnabled && this.m_users == null)
                {
                    var dataLayer = ApplicationContext.Current.GetService<IDataPersistenceService<SecurityUser>>();
                    this.m_users = dataLayer.Query(u => u.Roles.Any(r => r.Key == this.Key), null).ToList();
                }
                return this.m_users;
            }
        }

        /// <summary>
        /// Force delay load properties to be reloaded from the data store
        /// </summary>
        public override void Refresh()
        {
            base.Refresh();
            this.m_users = null;
        }

    }
}
