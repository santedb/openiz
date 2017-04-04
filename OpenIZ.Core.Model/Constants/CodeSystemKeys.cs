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
 * User: justi
 * Date: 2016-6-14
 */
using System;

namespace OpenIZ.Core.Model.Constants
{
	/// <summary>
	/// Code system identifiers
	/// </summary>
	public static class CodeSystemKeys
	{
		/// <summary>
		/// Parse CVX
		/// </summary>
		public static readonly Guid CVX = Guid.Parse("eba4f94a-2cad-4bb3-aca7-f4e54eaac4bd");

		/// <summary>
		/// ICD10
		/// </summary>
		public static readonly Guid ICD10 = Guid.Parse("F7A5CBD8-5425-415E-8308-D14B94F56917");

		/// <summary>
		/// ICD-10 CM
		/// </summary>
		public static readonly Guid ICD10CM = Guid.Parse("ED9742E5-FA5B-4644-9FB5-2F935ED08B1E");

		/// <summary>
		/// ICD9
		/// </summary>
		public static readonly Guid ICD9 = Guid.Parse("51EA1E1B-EDC0-455A-A72B-9076860E284D");

		/// <summary>
		/// ISO-639-1
		/// </summary>
		public static readonly Guid ISO6391 = Guid.Parse("EB04FE20-BBBC-4C70-9EEF-045BC4F70982");

		/// <summary>
		/// ISO639-2
		/// </summary>
		public static readonly Guid ISO6392 = Guid.Parse("089044EA-DD41-4258-A497-E6247DD364F6");

		/// <summary>
		/// LOINC
		/// </summary>
		public static readonly Guid LOINC = Guid.Parse("08C59397-706B-456A-AEB1-9E7D5A2ADC94");

		/// <summary>
		/// SNOMED-CT
		/// </summary>
		public static readonly Guid SNOMEDCT = Guid.Parse("B3030751-D4DB-420B-B765-E837607820CD");

		/// <summary>
		/// UCUM
		/// </summary>
		public static readonly Guid UCUM = Guid.Parse("4853A702-FFF3-4EFB-8DD7-54AACCA53664");
	}
}