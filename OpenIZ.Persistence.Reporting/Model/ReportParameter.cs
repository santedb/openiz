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
 * User: khannan
 * Date: 2017-1-6
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenIZ.Persistence.Reporting.Model
{
	/// <summary>
	/// Represents a report parameter.
	/// </summary>
	[Table("report_parameter")]
	public class ReportParameter
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ReportParameter"/> class.
		/// </summary>
		public ReportParameter()
		{
			this.DefaultValues = new List<ParameterValue>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ReportParameter"/> class
		/// with a specific <see cref="Core.Model.RISI.ReportParameter"/> instance.
		/// </summary>
		/// <param name="reportParameter">The report parameter instance.</param>
		public ReportParameter(Core.Model.RISI.ReportParameter reportParameter)
		{
			this.DataType = reportParameter.DataType.Key.Value;
			this.DefaultValue = new ParameterValue(reportParameter.Value);
			this.DefaultValues = new List<ParameterValue>();
			this.Id = reportParameter.Key.Value;
			this.IsNullable = reportParameter.IsNullable;
			this.Name = reportParameter.Name;
		}

		/// <summary>
		/// Gets or sets the creation time of the parameter.
		/// </summary>
		[Required]
		[Column("creation_time")]
		public DateTimeOffset CreationTime { get; set; }

		/// <summary>
		/// Gets or sets the data type of the report parameter.
		/// </summary>
		[Required]
		[Column("data_type")]
		public Guid DataType { get; set; }

		/// <summary>
		/// Gets or sets the default value of the report parameter.
		/// </summary>
		[Column("default_value")]
		public ParameterValue DefaultValue { get; set; }

		/// <summary>
		/// Gets or sets the default values associated with the report parameter.
		/// </summary>
		public virtual ICollection<ParameterValue> DefaultValues { get; set; }

		/// <summary>
		/// Gets or sets the id of the parameter.
		/// </summary>
		[Key]
		[Column("id")]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public Guid Id { get; set; }

		/// <summary>
		/// Gets or sets whether the report parameter is nullable.
		/// </summary>
		[Required]
		[Column("is_nullable")]
		public bool IsNullable { get; set; }

		/// <summary>
		/// Gets or sets the name of the report parameter.
		/// </summary>
		[Required]
		[Column("name")]
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the report reference associated with the report parameter.
		/// </summary>
		[ForeignKey("ReportId")]
		public virtual ReportDefinition ReportDefinition { get; set; }

		/// <summary>
		/// Gets or sets the report ID associated with the report parameter.
		/// </summary>
		[Required]
		[Column("report_id")]
		public Guid ReportId { get; set; }
	}
}