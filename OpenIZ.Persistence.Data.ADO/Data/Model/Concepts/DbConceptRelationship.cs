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
 * Date: 2016-6-14
 */
using OpenIZ.Persistence.Data.ADO.Data.Attributes;
using System;

namespace OpenIZ.Persistence.Data.ADO.Data.Model.Concepts
{
	/// <summary>
	/// Represents concept relationships
	/// </summary>
	[Table("cd_rel_assoc_tbl")]
	public class DbConceptRelationship : DbVersionedAssociation
	{
        /// <summary>
        /// Get the identifier of the key
        /// </summary>
        [Column("cd_rel_id"), PrimaryKey, AutoGenerated]
        public override Guid Key { get; set; }

        /// <summary>
        /// Gets or sets the relationship type identifier.
        /// </summary>
        /// <value>The relationship type identifier.</value>
        [Column("rel_typ_id"), ForeignKey(typeof(DbConceptRelationshipType), nameof(DbConceptRelationshipType.Key))]
		public Guid RelationshipTypeKey {
			get;
			set;
		}

        /// <summary>
        /// Gets or sets the source act key
        /// </summary>
        [Column("src_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.Key))]
        public override Guid SourceKey { get; set; }

        /// <summary>
        /// Gets or sets the target entity key
        /// </summary>
        [Column("trg_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.Key))]
        public Guid TargetKey { get; set; }
    }
}

