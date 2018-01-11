﻿/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
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
using OpenIZ.OrmLite.Attributes;
using OpenIZ.Persistence.Data.ADO.Data.Model.Concepts;
using OpenIZ.Persistence.Data.ADO.Data.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Persistence.Data.ADO.Data.Model.Acts
{
    /// <summary>
    /// Represents a link between an act and an entity
    /// </summary>
    [Table("act_ptcpt_tbl")]
    public class DbActParticipation : DbActVersionedAssociation
    {
        /// <summary>
        /// Gets or sets the primary key
        /// </summary>
        [Column("act_ptcpt_id"), PrimaryKey, AutoGenerated]
        public override Guid Key { get; set; }

        /// <summary>
        /// Gets or sets the role that the player plays in the act
        /// </summary>
        [Column("rol_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.Key)), AlwaysJoin]
        public Guid ParticipationRoleKey { get; set; }

        /// <summary>
        /// Gets or sets the quantity
        /// </summary>
        [Column("qty")]
        public int? Quantity { get; set; }

        /// <summary>
        /// Target entity key
        /// </summary>
        [Column("ent_id"), ForeignKey(typeof(DbEntity), nameof(DbEntity.Key))]
        public Guid TargetKey { get; set; }

        /// <summary>
        /// Represents the sequencing
        /// </summary>
        [Column("ptcpt_seq_id"), AutoGenerated]
        public int SequenceId { get; set; }
    }
}
