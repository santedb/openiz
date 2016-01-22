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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MARC.HI.EHRS.SVC.Core.Data;
using System.Runtime.Serialization;
using System.ComponentModel;
using OpenIZ.Core.Model.Attributes;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;

namespace OpenIZ.Core.Model
{
    /// <summary>
    /// Represents versioned based data, that is base data which has versions
    /// </summary>
    [Serializable]
    [DataContract(Name = "VersionedEntityData", Namespace = "http://openiz.org/model")]
    public abstract class VersionedEntityData<THistoryModelType> : BaseEntityData where THistoryModelType : VersionedEntityData<THistoryModelType>
    {

        // Previous version id
        private Guid? m_previousVersionId;
        // Previous version
        
        private THistoryModelType m_previousVersion;

        /// <summary>
        /// Creates a new versioned base data class
        /// </summary>
        public VersionedEntityData()
        {
        }

        /// <summary>
        /// Gets or sets the previous version key
        /// </summary>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DataMember(Name = "previousVersionRef")]
        public virtual Guid? PreviousVersionKey
        {
            get
            {
                return this.m_previousVersionId;
            }
            set
            {
                this.m_previousVersionId = value;
                this.m_previousVersion = default(THistoryModelType);
            }
        }

        /// <summary>
        /// Gets or sets the previous version
        /// </summary>
        [DelayLoad]
        [IgnoreDataMember]
        public virtual THistoryModelType PreviousVersion
        {
            get
            {
                this.m_previousVersion = base.DelayLoad(this.m_previousVersionId, this.m_previousVersion);
                return this.m_previousVersion;
            }
            set
            {
                this.m_previousVersion = value;
                if (value == default(THistoryModelType))
                    this.m_previousVersionId = null;
                else
                    this.m_previousVersionId = value.VersionKey;
            }
        }

        /// <summary>
        /// Gets or sets the key which represents the version of the entity
        /// </summary>
        [DataMember(Name = "versionRef")]
        public Guid VersionKey { get; set; }

        /// <summary>
        /// The sequence number of the version (for ordering)
        /// </summary>
        [DataMember(Name = "sequenceRef")]
        public Decimal VersionSequence { get; set; }

        /// <summary>
        /// Gets or sets the IIdentified data for this object
        /// </summary>
        [IgnoreDataMember]
        public override Identifier<Guid> Id
        {
            get
            {
                var retVal = base.Id;
                retVal.VersionId = this.VersionKey;
                return retVal;
            }
            set
            {
                base.Id = value;
                this.VersionKey = value.VersionId;
            }
        }

        /// <summary>
        /// Represent the versioned data as a string
        /// </summary>
        public override string ToString()
        {
            return String.Format("{0} (K:{1}, V:{2})", this.GetType().Name, this.Key, this.VersionKey);
        }

        /// <summary>
        /// Force bound attributes to reload
        /// </summary>
        public override void Refresh()
        {
            base.Refresh();
            this.m_previousVersion = default(THistoryModelType);
        }
    }

}
