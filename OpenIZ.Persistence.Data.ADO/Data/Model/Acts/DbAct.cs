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
using OpenIZ.Persistence.Data.ADO.Data.Attributes;
using OpenIZ.Persistence.Data.ADO.Data.Model.Extensibility;
using OpenIZ.Persistence.Data.ADO.Data.Model.Concepts;

namespace OpenIZ.Persistence.Data.ADO.Data.Model.Acts
{
    /// <summary>
    /// Represents a table which can store act data
    /// </summary>
    [Table("act_tbl")]
    public class DbAct : DbIdentified
    {

        /// <summary>
        /// Gets or sets the template
        /// </summary>
        [Column("tpl_id"), ForeignKey(typeof(DbTemplateDefinition), nameof(DbTemplateDefinition.Key))]
        public Guid TemplateKey { get; set; }

        /// <summary>
        /// Identifies the class concept
        /// </summary>
        [Column("cls_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.Key))]
        public Guid ClassConceptKey { get; set; }

        /// <summary>
        /// Gets or sets the mood of the act
        /// </summary>
        [Column("mod_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.Key))]
        public Guid MoodConceptKey { get; set; }

        /// <summary>
        /// Gets or sets the key
        /// </summary>
        [Column("act_id"), PrimaryKey]
        public override Guid Key { get; set; }
    }

}
