/*
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
using OpenIZ.OrmLite;
using System.Data.Common;
using OpenIZ.Core.Diagnostics;

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


        /// <summary>
        /// Return true if the specified object exists
        /// </summary>
        public override bool Exists(DataContext context, Guid key)
        {
            return context.Any<TDomain>(o => o.Key == key);
        }

        #region implemented abstract members of LocalDataPersistenceService


        /// <summary>
        /// Performthe actual insert.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public override TModel InsertInternal(DataContext context, TModel data, IPrincipal principal)
        {
            try
            {
                var domainObject = this.FromModelInstance(data, context, principal) as TDomain;

                domainObject = context.Insert<TDomain>(domainObject);
                data.Key = domainObject.Key;

                return data;
            }
            catch (DbException ex)
            {
                this.m_tracer.TraceEvent(System.Diagnostics.TraceEventType.Error, ex.HResult, "Error inserting {0} - {1}", data, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Perform the actual update.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public override TModel UpdateInternal(DataContext context, TModel data, IPrincipal principal)
        {
            try
            {
                // Sanity 
                if (data.Key == Guid.Empty)
                    throw new AdoFormalConstraintException(AdoFormalConstraintType.NonIdentityUpdate);

                // Map and copy
                var newDomainObject = this.FromModelInstance(data, context, principal) as TDomain;
                var oldDomainObject = context.SingleOrDefault<TDomain>(o => o.Key == newDomainObject.Key);
                if (oldDomainObject == null)
                    throw new KeyNotFoundException(data.Key.ToString());

                oldDomainObject.CopyObjectData(newDomainObject);
                context.Update<TDomain>(oldDomainObject);
                return data;
            }
            catch (DbException ex)
            {
                this.m_tracer.TraceEvent(System.Diagnostics.TraceEventType.Error, ex.HResult, "Error updating {0} - {1}", data, ex.Message);
                throw;
            }

        }

        /// <summary>
        /// Performs the actual obsoletion
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public override TModel ObsoleteInternal(DataContext context, TModel data, IPrincipal principal)
        {
            try
            {
                if (data.Key == Guid.Empty)
                    throw new AdoFormalConstraintException(AdoFormalConstraintType.NonIdentityUpdate);

                var domainObject = context.FirstOrDefault<TDomain>(o => o.Key == data.Key);

                if (domainObject == null)
                    throw new KeyNotFoundException(data.Key.ToString());

                context.Delete(domainObject);

                return data;
            }
            catch (DbException ex)
            {
                this.m_tracer.TraceEvent(System.Diagnostics.TraceEventType.Error, ex.HResult, "Error obsoleting {0} - {1}", data, ex.Message);
                throw;
            }

        }

        /// <summary>
        /// Performs the actual query
        /// </summary>
        public override IEnumerable<TModel> QueryInternal(DataContext context, Expression<Func<TModel, bool>> query, Guid queryId, int offset, int? count, out int totalResults, IPrincipal principal, bool countResults = false)
        {
            return this.DoQueryInternal(context, query, queryId, offset, count, out totalResults, countResults)
                .ToList()
                .Select(o => o is Guid ? this.Get(context, (Guid)o, principal) : this.CacheConvert(o, context, principal));
        }

        /// <summary>
        /// Get the specified object
        /// </summary>
        internal override TModel Get(DataContext context, Guid key, IPrincipal principal)
        {
            var cacheService = new AdoPersistenceCache(context);
            var retVal = cacheService?.GetCacheItem<TModel>(key);
            if (retVal != null)
            {
                this.m_tracer.TraceVerbose("Object {0} found in cache", retVal);
                return retVal;
            }
            else
            {
                this.m_tracer.TraceVerbose("Object {0}({1}) not found in cache will load", typeof(TModel).FullName, key);
                return this.CacheConvert(context.FirstOrDefault<TDomain>(o => o.Key == key), context, principal);
            }
        }


        /// <summary>
        /// Obsolete the specified objects 
        /// </summary>
        protected override void BulkObsoleteInternal(DataContext context, IPrincipal principal, Guid[] keysToObsolete)
        {
            context.Delete<TDomain>(o => keysToObsolete.Contains(o.Key));
        }

        /// <summary>
        /// Purge the specified object 
        /// </summary>
        protected override void BulkPurgeInternal(DataContext connection, IPrincipal principal, Guid[] keysToPurge)
        {
            // TODO: CASCADE DELETE - SCAN THE CONTEXT DOMAIN FOR TABLES WHICH POINT AT TDOMAIN 
            // AND CASCADE THE DELETION TO THEM WHERE THE FK THAT POINTS AT MY TABLE 
            // IS IN THE KEYS PROVIDED
            var ofs = 0;
            while (ofs < keysToPurge.Length)
            {
                var keys = keysToPurge.Skip(ofs).Take(100).ToArray();
                ofs += 100;
                connection.Delete<TDomain>(o => keys.Contains(o.Key));
            }
        }

        /// <summary>
        /// Perform the query for bulk keys with an open context
        /// </summary>
        protected override IEnumerable<Guid> QueryKeysInternal(DataContext context, Expression<Func<TModel, bool>> query, int offset, int? count, out int totalResults)
        {
            // Construct the SQL query
            var pk = TableMapping.Get(typeof(TDomain)).Columns.SingleOrDefault(o => o.IsPrimaryKey);
            var domainQuery = AdoPersistenceService.GetQueryBuilder().CreateQuery(query, pk);

            var results = context.Query<Guid>(domainQuery);

            count = count ?? 100;
            if (m_configuration.UseFuzzyTotals)
            {
                // Skip and take
                results = results.Skip(offset).Take(count.Value + 1);
                totalResults = offset + results.Count();
            }
            else
            {
                totalResults = results.Count();
                results = results.Skip(offset).Take(count.Value);
            }

            return results.ToList(); // exhaust the results and continue
        }

        /// <summary>
        /// Copy the specified keys
        /// </summary>
        public override void Copy(Guid[] keysToCopy, DataContext fromContext, DataContext toContext)
        {
            var ofs = 0;
            while (ofs < keysToCopy.Length)
            {
                var keys = keysToCopy.Skip(ofs).Take(100).ToArray();
                ofs += 100;
                toContext.InsertOrUpdate(fromContext.Query<TDomain>(o => keys.Contains(o.Key)));
            }
        }

        #endregion
    }
}

