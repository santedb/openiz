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
 * Date: 2016-8-2
 */
using System;
using OpenIZ.Core.Model.Security;
using System.Linq.Expressions;
using System.Linq;
using OpenIZ.Core.Model;
using System.Text;
using System.Collections.Generic;
using OpenIZ.Persistence.Data.ADO.Data.Model;
using System.Security.Principal;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using OpenIZ.Core.Model.Interfaces;
using OpenIZ.Core;
using System.Reflection;
using OpenIZ.Core.Services;
using OpenIZ.Persistence.Data.ADO.Data;
using OpenIZ.Persistence.Data.ADO.Exceptions;
using OpenIZ.Persistence.Data.ADO.Util;

namespace OpenIZ.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Generic persistnece service for identified entities
    /// </summary>
    public abstract class IdentifiedPersistenceService<TModel, TDomain> : IdentifiedPersistenceService<TModel, TDomain, TDomain>
        where TModel : IdentifiedData, new()
        where TDomain : class, IDbIdentified, new()
    { }

    /// <summary>
    /// Generic persistence service which can persist between two simple types.
    /// </summary>
    public abstract class IdentifiedPersistenceService<TModel, TDomain, TQueryResult> : CorePersistenceService<TModel, TDomain, TQueryResult> 
		where TModel : IdentifiedData, new()
		where TDomain : class, IDbIdentified, new()
	{

        #region implemented abstract members of LocalDataPersistenceService

        
		/// <summary>
		/// Performthe actual insert.
		/// </summary>
		/// <param name="context">Context.</param>
		/// <param name="data">Data.</param>
		public override TModel Insert (DataContext context, TModel data, IPrincipal principal)
		{
			var domainObject = this.FromModelInstance (data, context, principal) as TDomain;

            domainObject = context.Insert<TDomain>(domainObject);
            data.Key = domainObject.Key;

            return data;
		}

        /// <summary>
        /// Perform the actual update.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public override TModel Update (DataContext context, TModel data, IPrincipal principal)
		{
            // Sanity 
            if(data.Key == Guid.Empty)
                throw new AdoFormalConstraintException(AdoFormalConstraintType.NonIdentityUpdate);

            // Map and copy
            var newDomainObject = this.FromModelInstance (data, context, principal) as TDomain;
            var oldDomainObject = context.SingleOrDefault<TDomain>(o=>o.Key == newDomainObject.Key);
            if (oldDomainObject == null)
                throw new KeyNotFoundException(data.Key.ToString());

            oldDomainObject.CopyObjectData(newDomainObject);
            context.Update<TDomain>(oldDomainObject);
			return data;
		}

        /// <summary>
        /// Performs the actual obsoletion
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public override TModel Obsolete (DataContext context, TModel data, IPrincipal principal)
		{
            if (data.Key == Guid.Empty)
                throw new AdoFormalConstraintException(AdoFormalConstraintType.NonIdentityUpdate);

            var domainObject = context.FirstOrDefault<TDomain>(o => o.Key == data.Key);

            if (domainObject == null)
                throw new KeyNotFoundException(data.Key.ToString());

			context.Delete(domainObject);

			return data;
		}

        /// <summary>
        /// Performs the actual query
        /// </summary>
        public override IEnumerable<TModel> Query (DataContext context, Expression<Func<TModel, bool>> query, int offset, int? count, out int totalResults, IPrincipal principal)
		{
            return this.QueryInternal(context, query, offset, count, out totalResults).Select(o=>this.CacheConvert(o, context, principal));
		}

        
        #endregion
    }
}

