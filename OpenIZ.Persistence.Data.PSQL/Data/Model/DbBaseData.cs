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
 * Date: 2016-8-2
 */
using PetaPoco;
using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Persistence.Data.PSQL.Data.Model
{
    /// <summary>
    /// Represents base data
    /// </summary>
    public abstract class DbBaseData : DbVersionedAssociation
    {
        /// <summary>
        /// Gets or sets the entity id which created this
        /// </summary>
        [Column("crt_usr_id")]
        public Guid CreatedBy { get; set; }
        /// <summary>
        /// Gets or sets the id which obsoleted this
        /// </summary>
        [Column("obslt_usr_id")]
        public Guid? ObsoletedBy { get; set; }
        /// <summary>
        /// Gets or sets the creation time
        /// </summary>
        [Column("crt_utc")]
        public DateTimeOffset CreationTime { get; set; }
        /// <summary>
        /// Gets or sets the obsoletion time
        /// </summary>
        [Column("obslt_utc")]
        public DateTimeOffset? ObsoletionTime { get; set; }
    }

    /// <summary>
    /// Non-versioned base data
    /// </summary>
    public abstract class DbNonVersionedBaseData : DbBaseData
    {

        /// <summary>
        /// Gets or sets the updated user
        /// </summary>
        [Column("upd_usr_id")]
        public Guid? UpdatedBy { get; set; }

        /// <summary>
        /// Gets or sets the time of updating
        /// </summary>
        [Column("upd_utc")]
        public DateTimeOffset? UpdatedTime { get; set; }
    }
    
}