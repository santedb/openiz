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
	/// Concept set
	/// </summary>
	[Table("cd_set_tbl")]
	public class DbConceptSet : DbNonVersionedBaseData
	{
		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// <value>The name.</value>
		[Column("set_name")]
		public String Name {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the mnemonic.
		/// </summary>
		/// <value>The mnemonic.</value>
		[Column("mnemonic")]
		public String Mnemonic {
			get;
			set;
		}

        /// <summary>
        /// Gets or sets the oid of the concept set
        /// </summary>
        [Column("oid")]
        public String Oid { get; set; }

        /// <summary>
        /// Gets or sets the url of the concept set
        /// </summary>
        [Column("url")]
        public String Url { get; set; }

        /// <summary>
        /// Gets or sets the id
        /// </summary>
        [Column("set_id"), PrimaryKey]
        public override Guid Key { get; set; }
    }

	/// <summary>
	/// Concept set concept association.
	/// </summary>
	[Table("cd_set_mem_assoc_tbl")]
	public class DbConceptSetConceptAssociation 
	{

		/// <summary>
		/// Gets or sets the concept identifier.
		/// </summary>
		/// <value>The concept identifier.</value>
		[Column("cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.Key)), PrimaryKey]
		public Guid ConceptKey {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the concept set identifier.
		/// </summary>
		/// <value>The concept set identifier.</value>
		[Column("set_id"), ForeignKey(typeof(DbConceptSet), nameof(DbConceptSet.Key)), PrimaryKey]
		public Guid ConceptSetKey {
			get;
			set;
		}

       
    }
}

