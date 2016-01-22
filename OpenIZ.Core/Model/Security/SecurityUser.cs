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
using MARC.HI.EHRS.SVC.Core.Data;
using MARC.HI.EHRS.SVC.Core.Services;
using OpenIZ.Core.Model.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Core.Model.Security
{
    /// <summary>
    /// Security user represents a user for the purpose of security 
    /// </summary>
    [XmlType("SecurityUser", Namespace = "http://openiz.org/model")]
    [Serializable]
    [XmlRoot(Namespace = "http://openiz.org/model", ElementName = "SecurityUser")]
    public class SecurityUser : SecurityEntity
    {

        
        // Roles
        
        private List<SecurityRole> m_roles;
        // The updated by id
        private Guid? m_updatedById;
        // The updated by user
        
        private SecurityUser m_updatedBy;
        // Policies
        
        private List<SecurityPolicyInstance> m_policies;
        /// <summary>
        /// Gets or sets the email address of the user
        /// </summary>
        [XmlElement("email")]
        public String Email { get; set; }
        /// <summary>
        /// Gets or sets whether the email address is confirmed
        /// </summary>
        [XmlElement("emailConfirmed")]
        public Boolean EmailConfirmed { get; set; }
        /// <summary>
        /// Gets or sets the number of invalid login attempts by the user
        /// </summary>
        [XmlElement("invalidLoginAttempts")]
        public Int32 InvalidLoginAttempts { get; set; }
        /// <summary>
        /// Gets or sets whether the account is locked out
        /// </summary>
        [XmlElement("lockoutEnabled")]
        public Boolean LockoutEnabled { get; set; }
        /// <summary>
        /// Gets or sets whether the password hash is enabled
        /// </summary>
        [XmlElement("passwordHash")]
        public String PasswordHash { get; set; }
        /// <summary>
        /// Gets or sets whether the security has is enabled
        /// </summary>
        [XmlElement("securityStamp")]
        public String SecurityHash { get; set; }
        /// <summary>
        /// Gets or sets whether two factor authentication is required
        /// </summary>
        [XmlElement("twoFactorEnabled")]
        public Boolean TwoFactorEnabled { get; set; }
        /// <summary>
        /// Gets or sets the logical user name ofthe user
        /// </summary>
        [XmlElement("userName")]
        public String UserName { get; set; }
        /// <summary>
        /// Gets or sets the binary representation of the user's photo
        /// </summary>
        [XmlElement("photo")]
        public byte[] UserPhoto { get; set; }
        /// <summary>
        /// The last login time
        /// </summary>
        [XmlElement("lastLoginTime")]
        public DateTime LastLoginTime { get; set; }
        /// <summary>
        /// Represents roles
        /// </summary>
        [XmlIgnore]
        [DelayLoad(null)]
        public List<SecurityRole> Roles {
            get
            {
                if(this.IsDelayLoadEnabled && this.m_roles == null)
                {
                    var dataLayer = ApplicationContext.Current.GetService<IDataPersistenceService<SecurityRole>>();
                    this.m_roles = dataLayer.Query(r => r.Users.Any(u => u.Key == this.Key), null).ToList();
                }
                return this.m_roles;
            }
        }
        /// <summary>
        /// Updated time
        /// </summary>
        [XmlElement("updatedTime")]
        public DateTime? UpdatedTime { get; set; }

        /// <summary>
        /// Gets or sets the user that updated this base data
        /// </summary>
        [DelayLoad(null)]
        [XmlIgnore]
        public SecurityUser UpdatedBy
        {
            get
            {
                this.m_updatedBy = base.DelayLoad(this.m_updatedById, this.m_updatedBy);
                return this.m_updatedBy;
            }
        }

        /// <summary>
        /// Gets or sets the created by identifier
        /// </summary>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [XmlElement("updatedBy")]
        public Guid?  UpdatedByKey
        {
            get { return this.m_updatedById; }
            set
            {
                if (this.m_updatedById != value)
                    this.m_updatedBy = null;
                this.m_updatedById = value;
            }
        }

        /// <summary>
        /// Gets or sets the patient's phone number
        /// </summary>
        [XmlElement("phoneNumber")]
        public String PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets whether the phone number was confirmed
        /// </summary>
        [XmlElement("phoneNumberConfirmed")]
        public Boolean PhoneNumberConfirmed { get; set; }

        /// <summary>
        /// Get the policies active for this user
        /// </summary>
        [DelayLoad(null)]
        [XmlIgnore]
        public override List<SecurityPolicyInstance> Policies
        {
            get
            {
                if (this.m_policies == null && this.IsDelayLoadEnabled)
                {
                    var rolePolicies = this.Roles.SelectMany(o => o.Policies, (r, p) => p);
                    this.m_policies = new List<SecurityPolicyInstance>();
                    foreach (var itm in rolePolicies)
                    {
                        var existingRetVal = this.m_policies.Find(o => o.Policy.Key == itm.Policy.Key);
                        if (existingRetVal == null)
                            this.m_policies.Add(itm);
                        else if (existingRetVal.GrantType > itm.GrantType)
                        {
                            this.m_policies.Remove(existingRetVal);
                            this.m_policies.Add(itm);
                        }
                    }
                }
                return this.m_policies;
            }
        }

        /// <summary>
        /// Forces delay load properties to be from the database
        /// </summary>
        public override void Refresh()
        {
            base.Refresh();
            this.m_policies = null;
            this.m_roles = null;
            this.m_updatedBy = null;
        }

    }
}
