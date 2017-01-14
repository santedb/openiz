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
using PetaPoco;
using System;



namespace OpenIZ.Persistence.Data.PSQL.Data.Model.Extensibility
{
	/// <summary>
	/// Extension types
	/// </summary>
	[TableName("ext_typ_tbl")]
	public class DbExtensionType: DbNonVersionedBaseData
	{

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// <value>The name.</value>
		[Column("ext_name")]
		public String Name {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the extension handler.
		/// </summary>
		/// <value>The extension handler.</value>
		[Column("hdlr_cls")]
		public String ExtensionHandler {
			get;
			set;
		}

        /// <summary>
        /// Whether the extension should be executed
        /// </summary>
        [Column("is_active")]
        public bool IsActive { get; set; }

        /// <summary>
        /// Extension type key
        /// </summary>
        [Column("ext_typ_id")]
        public override Guid Key { get; set; }
    }
}

