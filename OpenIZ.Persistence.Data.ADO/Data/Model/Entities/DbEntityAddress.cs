﻿/*
 * Copyright 2015-2017 Mohawk College of Applied Arts and Technology
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
 * Date: 2017-1-14
 */
using OpenIZ.OrmLite.Attributes;
using OpenIZ.Persistence.Data.ADO.Data.Model.Concepts;
using System;

namespace OpenIZ.Persistence.Data.ADO.Data.Model.Entities
{
	/// <summary>
	/// Represents one or more entity addresses linked to an Entity
	/// </summary>
	[Table("ent_addr_tbl")]
	public class DbEntityAddress : DbEntityVersionedAssociation
	{
        /// <summary>
        /// Gets or sets the key
        /// </summary>
        [Column("addr_id"), PrimaryKey, AutoGenerated]
        public override Guid Key { get; set; }

        /// <summary>
        /// Gets or sets the use concept identifier.
        /// </summary>
        /// <value>The use concept identifier.</value>
        [Column("use_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.Key))]
		public Guid UseConceptKey {
			get;
			set;
		}

	}

	/// <summary>
	/// Represents an identified address component
	/// </summary>
	[Table("ent_addr_cmp_tbl")]
	public class DbEntityAddressComponent : DbGenericNameComponent
	{

		/// <summary>
		/// Gets or sets the address identifier.
		/// </summary>
		/// <value>The address identifier.</value>
		[Column("addr_id"), ForeignKey(typeof(DbEntityAddress), nameof(DbEntityAddress.Key))]
		public override Guid SourceKey  {
			get;
			set;
		}

        /// <summary>
        /// Gets or sets the address value
        /// </summary>
        [Column("val_id"), ForeignKey(typeof(DbEntityAddressComponentValue), nameof(DbEntityAddressComponentValue.Key)), AlwaysJoin]
        public override Guid ValueKey { get; set; }
    }

    /// <summary>
    /// Gets or sets the entity address component value
    /// </summary>
    [Table("ent_addr_cmp_val_tbl")]
    public class DbEntityAddressComponentValue : DbIdentified
    {
        /// <summary>
        /// Gets or sets the pk of the val
        /// </summary>
        [Column("val_id"), PrimaryKey, AutoGenerated]
        public override Guid Key { get; set; }

        /// <summary>
        /// Gets or sets the value
        /// </summary>
        [Column("val")]
        public String Value { get; set; }
        
    }

}

