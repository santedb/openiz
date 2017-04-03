﻿/*
 * Copyright 2015-2017 Mohawk College of Applied Arts and Technology
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
 * User: Nityan
 * Date: 2017-4-2
 */

using System.Xml.Serialization;

namespace OpenIZ.Messaging.CSD.Model.Core
{
	/// <summary>
	/// Represents a unique id.
	/// </summary>
	[XmlInclude(typeof(Service))]
	[XmlInclude(typeof(Provider))]
	[XmlInclude(typeof(Facility))]
	[XmlInclude(typeof(Organization))]
	[XmlType("uniqueId", Namespace = "urn:ihe:iti:csd:2013")]
	public class UniqueId
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UniqueId"/> class.
		/// </summary>
		public UniqueId()
		{
			
		}

		/// <summary>
		/// Gets or sets the entity identifier.
		/// </summary>
		/// <value>The entity identifier.</value>
		[XmlAttribute("entityID")]
		public string EntityId { get; set; }
	}
}
