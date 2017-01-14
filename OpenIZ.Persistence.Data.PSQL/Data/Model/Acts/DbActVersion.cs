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
 * Date: 2016-7-1
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PetaPoco;

namespace OpenIZ.Persistence.Data.PSQL.Data.Model.Acts
{
    /// <summary>
    /// Represents a table which can store act data
    /// </summary>
    [TableName("act_vrsn_id")]
    public class DbActVersion : DbVersionedData
    {
        /// <summary>
        /// True if negated
        /// </summary>
        [Column("neg_ind")]
        public bool IsNegated { get; set; }

        /// <summary>
        /// Identifies the time that the act occurred
        /// </summary>
        [Column("act_utc")]
        public DateTime? ActTime { get; set; }

        /// <summary>
        /// Identifies the start time of the act
        /// </summary>
        [Column("act_start_utc")]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Identifies the stop time of the act
        /// </summary>
        [Column("act_stop_utc")]
        public DateTime? StopTime { get; set; }

        /// <summary>
        /// Gets or sets the reason concept
        /// </summary>
        [Column("rsn_cd_id")]
        public Guid ReasonConceptKey { get; set; }

        /// <summary>
        /// Gets or sets the status concept
        /// </summary>
        [Column("sts_cd_id")]
        public Guid StatusConceptKey { get; set; }

        /// <summary>
        /// Gets or sets the type concept
        /// </summary>
        [Column("typ_cd_id")]
        public Guid TypeConceptKey { get; set; }

        /// <summary>
        /// Version identifier
        /// </summary>
        [Column("act_vrsn_id")]
        public override Guid VersionId { get; set; }
        
        /// <summary>
        /// Gets or sets the act identifier
        /// </summary>
        [Column("act_id")]
        public override Guid Key { get; set; }
    }
}
