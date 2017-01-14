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
using System;



namespace OpenIZ.Persistence.Data.PSQL.Data.Model.Entities
{
	/// <summary>
	/// Represents a telecommunications address
	/// </summary>
	[TableName("entity_telecom")]
	public class DbTelecomAddress : DbEntityLink
	{

		/// <summary>
		/// Gets or sets the telecom use.
		/// </summary>
		/// <value>The telecom use.</value>
		[Column("use")]
		public Guid TelecomUseKey {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the value.
		/// </summary>
		/// <value>The value.</value>
		[Column("value")
		public String Value {
			get;
			set;
		}

	}
}
