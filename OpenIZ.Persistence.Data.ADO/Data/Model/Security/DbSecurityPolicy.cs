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



namespace OpenIZ.Persistence.Data.ADO.Data.Model.Security
{
	/// <summary>
	/// Represents a single security policy
	/// </summary>
	[Table("sec_pol_tbl")]
	public class DbSecurityPolicy : DbBaseData
	{

		/// <summary>
		/// Gets or sets the handler.
		/// </summary>
		/// <value>The handler.</value>
		[Column("hdlr_cls")]
		public String Handler {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// <value>The name.</value>
		[Column("pol_name")]
		public String Name {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is public.
		/// </summary>
		/// <value><c>true</c> if this instance is public; otherwise, <c>false</c>.</value>
		[Column("is_pub")]
		public bool IsPublic {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance can override.
		/// </summary>
		/// <value><c>true</c> if this instance can override; otherwise, <c>false</c>.</value>
		[Column("is_elev")]
		public bool CanOverride {
			get;
			set;
		}

        /// <summary>
        /// Gets or sets the policy oid
        /// </summary>
        [Column("oid")]
        public String Oid
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the primary key
        /// </summary>
        [Column("pol_id"), PrimaryKey]
        public override Guid Key { get; set; }

    }
}

