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
 * Date: 2016-8-17
 */

using OpenIZ.Persistence.Data.ADO.Data.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Persistence.Data.ADO.Data.Model.Extensibility
{
    /// <summary>
    /// Represents a database template definition
    /// </summary>
    [Table("tpl_def_tbl")]
    public class DbTemplateDefinition : DbBaseData
    {
        /// <summary>
        /// Gets or sets the key
        /// </summary>
        [Column("tpl_id"), PrimaryKey]
        public override Guid Key { get; set; }

        /// <summary>
        /// Gets the OID of the template
        /// </summary>
        [Column("oid")]
        public String Oid { get; set; }

        /// <summary>
        /// Gets the name of the template
        /// </summary>
        [Column("tpl_name")]
        public String Name { get; set; }

        /// <summary>
        /// Gets the mnemonic
        /// </summary>
        [Column("mnemonic")]
        public String Mnemonic { get; set; }

        /// <summary>
        /// Gets the mnemonic
        /// </summary>
        [Column("descr")]
        public String Description { get; set; }

    }
}