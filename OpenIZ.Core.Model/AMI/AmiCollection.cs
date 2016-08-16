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
 * User: Nityan
 * Date: 2016-8-14
 */
using System.Collections.Generic;
using System.Xml.Serialization;

namespace OpenIZ.Core.Model.AMI.Security
{
	/// <summary>
	/// AMI collection base
	/// </summary>
	[XmlType(Namespace = "http://openiz.org/ami")]
	public class AmiCollection<T>
	{
		/// <summary>
		/// Total collection size
		/// </summary>
		[XmlAttribute("size")]
		public int Size { get; set; }

		/// <summary>
		/// Total offset
		/// </summary>
		[XmlAttribute("offset")]
		public int Offset { get; set; }

		/// <summary>
		/// Collection item
		/// </summary>
		[XmlElement("item")]
		public List<T> CollectionItem { get; set; }
	}
}