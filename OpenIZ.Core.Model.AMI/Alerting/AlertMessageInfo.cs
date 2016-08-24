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
 * User: khannan
 * Date: 2016-8-24
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace OpenIZ.Core.Model.AMI.Alerting
{
	/// <summary>
	/// Represents information about an alert message.
	/// </summary>
	[XmlType(nameof(AlertMessageInfo), Namespace = "http://openiz.org/ami")]
	[XmlRoot(nameof(AlertMessageInfo), Namespace = "http://openiz.org/ami")]
	public class AlertMessageInfo
	{
		public AlertMessageInfo()
		{

		}

		//[XmlElement("alertInfo")]
		//public AlertMessage AlertMessage { get; set; }

		/// <summary>
		/// Gets or sets the id of the alert message.
		/// </summary>
		[XmlElement("id")]
		public Guid Id { get; set; }
	}
}
