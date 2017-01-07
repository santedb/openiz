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
 * Date: 2017-1-5
 */

using OpenIZ.Reporting.Core.Attributes;

namespace OpenIZ.Reporting.Core
{
	/// <summary>
	/// Represents a collection of report formats.
	/// </summary>
	public enum ReportFormat
	{
		/// <summary>
		/// The CSV report format.
		/// </summary>
		[FileExtension("csv")]
		Csv,

		/// <summary>
		/// The Microsoft Word Document report format.
		/// </summary>
		[FileExtension("docx")]
		Docx,

		/// <summary>
		/// The HTML report format.
		/// </summary>
		[FileExtension("html")]
		Html,

		/// <summary>
		/// The Jasper JPrint report format.
		/// </summary>
		[FileExtension("CSV")]
		JPrint,

		/// <summary>
		/// The Open Office Spreadsheet report format.
		/// </summary>
		[FileExtension("ods")]
		Ods,

		/// <summary>
		/// The Open Office Document report format.
		/// </summary>
		[FileExtension("odt")]
		Odt,

		/// <summary>
		/// The PDF report format.
		/// </summary>
		[FileExtension("pdf")]
		Pdf,

		/// <summary>
		/// The Rich Text Format report format.
		/// </summary>
		[FileExtension("rtf")]
		Rtf,

		/// <summary>
		/// The Microsoft Excel report format.
		/// </summary>
		[FileExtension("xls")]
		Xls,

		/// <summary>
		/// The Microsoft Excel (New) report format.
		/// </summary>
		[FileExtension("xlsx")]
		Xlsx,

		/// <summary>
		/// The XML report format.
		/// </summary>
		[FileExtension("xml")]
		Xml
	}
}