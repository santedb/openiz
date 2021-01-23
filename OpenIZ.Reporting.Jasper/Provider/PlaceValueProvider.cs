﻿/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: fyfej
 * Date: 2017-9-1
 */
using MARC.HI.EHRS.SVC.Core;
using OpenIZ.Core.Model;
using OpenIZ.Core.Model.Constants;
using OpenIZ.Core.Model.Entities;
using OpenIZ.Core.Model.RISI.Interfaces;
using OpenIZ.Core.Security;
using OpenIZ.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenIZ.Reporting.Jasper.Provider
{
	/// <summary>
	/// Represents a place value provider.
	/// </summary>
	public class PlaceValueProvider : IParameterValuesProvider
	{
		/// <summary>
		/// Gets or sets the query identifier.
		/// </summary>
		/// <value>The query identifier.</value>
		public Guid QueryId => Guid.Parse("1EECABF1-DF84-4CA7-80C7-245B2EE9C2C9");

		/// <summary>
		/// Gets a list of values.
		/// </summary>
		/// <typeparam name="T">The type of parameter for which to retrieve values.</typeparam>
		/// <returns>Returns a list of values.</returns>
		public IEnumerable<T> GetValues<T>() where T : IdentifiedData
		{
			var results = new List<Place>();

			var placePersistenceService = ApplicationContext.Current.GetService<IStoredQueryDataPersistenceService<Place>>();

			if (placePersistenceService == null)
			{
				throw new InvalidOperationException($"Unable to locate { nameof(IStoredQueryDataPersistenceService<Place>) }");
			}

			var totalCount = 0;
			var offset = 0;

			while (offset <= totalCount)
			{
				var places = placePersistenceService.Query(p => p.ObsoletionTime == null && p.ClassConceptKey == EntityClassKeys.Place && p.ObsoletionTime == null, this.QueryId, offset, 250, AuthenticationContext.Current.Principal, out totalCount);

				offset += 250;

				results.AddRange(places);
			}

			return results.Cast<T>();
		}
	}
}