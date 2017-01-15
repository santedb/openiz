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
 * Date: 2016-12-4
 */

using OpenIZ.Core.Model.RISI.Interfaces;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace OpenIZ.Core.Model.RISI
{
	/// <summary>
	/// Represents a parameter type.
	/// </summary>
	[XmlType(nameof(ParameterType), Namespace = "http://openiz.org/risi")]
	[XmlRoot(nameof(ParameterType), Namespace = "http://openiz.org/risi")]
	public class ParameterType : BaseEntityData
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ParameterType"/> class.
		/// </summary>
		public ParameterType()
		{
			this.ReportParameters = new List<ReportParameter>();
		}

		/// <summary>
		/// Gets or sets the auto-complete source.
		/// </summary>
		[XmlElement("listAutoComplete", Type = typeof(ListAutoCompleteSourceDefinition))]
		[XmlElement("queryAutoComplete", Type = typeof(QueryAutoCompleteSourceDefinition))]
		public AutoCompleteSourceDefinition AutoCompleteSourceDefinition { get; set; }

		/// <summary>
		/// Gets or sets a list of report parameters associated with the parameter type.
		/// </summary>
		[XmlElement("reportParameters")]
		public List<ReportParameter> ReportParameters { get; set; }

		/// <summary>
		/// Gets or sets the system type.
		/// </summary>
		[XmlIgnore]
		public Type SystemType { get; set; }

		/// <summary>
		/// Gets or sets the system type in XML.
		/// </summary>
		[XmlElement("systemType")]
		public string SystemTypeXml
		{
			get
			{
				return this.SystemType.AssemblyQualifiedName;
			}
			set
			{
				this.SystemType = System.Type.GetType(value);
			}
		}

		/// <summary>
		/// Gets or sets the values provider of the report data type.
		/// </summary>
		[XmlAttribute("provider")]
		public string ValuesProvider { get; set; }
	}
}