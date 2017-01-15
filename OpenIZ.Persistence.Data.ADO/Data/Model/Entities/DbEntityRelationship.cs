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
using OpenIZ.Persistence.Data.ADO.Data.Model.Concepts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Persistence.Data.ADO.Data.Model.Entities
{
    /// <summary>
    /// Represents a relationship between two entities
    /// </summary>
    [Table("ent_rel_tbl")] 
    public class DbEntityRelationship : DbVersionedAssociation
    {
        
        /// <summary>
        /// Gets or sets the link type concept
        /// </summary>
        [Column("rel_type_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.Key))]
        public Guid RelationshipTypeKey { get; set; }

        /// <summary>
        /// Quantity 
        /// </summary>
        [Column("qty")]
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the source entity id
        /// </summary>
        [Column("src_ent_id"), ForeignKey(typeof(DbEntity), nameof(DbEntity.Key))]
        public override Guid SourceKey { get; set; }

        /// <summary>
        /// Target entity key
        /// </summary>
        [Column("trg_ent_id"), ForeignKey(typeof(DbEntity), nameof(DbEntity.Key))]
        public Guid TargetKey { get; set; }

        /// <summary>
        /// Gets or sets the entity relationship id
        /// </summary>
        [Column("ent_rel_id"), PrimaryKey]
        public override Guid Key { get; set; }
    }
}
