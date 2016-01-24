﻿using OpenIZ.Core.Model.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.Text;
using System.Threading.Tasks;
using OpenIZ.Core.Model.Interfaces;

namespace OpenIZ.Core.Model
{
    /// <summary>
    /// Represents a bse class for bound relational data
    /// </summary>
    /// <typeparam name="TSourceType"></typeparam>
    [XmlType(Namespace = "http://openiz.org/model")]
    public abstract class Association<TSourceType> : BaseEntityData, ISimpleAssociation where TSourceType : IdentifiedData
    {

        // Target entity key
        private Guid m_sourceEntityKey;
        // The target entity
        
        private TSourceType m_sourceEntity;

        /// <summary>
        /// Gets or sets the source entity's key (where the relationship is FROM)
        /// </summary>
        [XmlElement("source")]
        public virtual Guid SourceEntityKey
        {
            get
            {
                return this.m_sourceEntityKey;
            }
            set
            {
                this.m_sourceEntityKey = value;
                this.m_sourceEntity = null;
            }
        }

        /// <summary>
        /// The entity that this relationship targets
        /// </summary>
        [DelayLoad(nameof(SourceEntityKey))]
        [XmlIgnore]
        public TSourceType SourceEntity
        {
            get
            {
                this.m_sourceEntity = this.DelayLoad(this.m_sourceEntityKey, this.m_sourceEntity);
                return this.m_sourceEntity;
            }
            set
            {
                this.m_sourceEntity = value;
                if (value == null)
                    this.m_sourceEntityKey = default(Guid);
                else
                    this.m_sourceEntityKey = value.Key;
            }
        }

        /// <summary>
        /// Force delay load properties to reload
        /// </summary>
        public override void Refresh()
        {
            base.Refresh();
            this.m_sourceEntity = null;
        }
    }
}
