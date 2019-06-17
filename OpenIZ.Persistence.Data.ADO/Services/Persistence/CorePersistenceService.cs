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
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using OpenIZ.Core.Model;
using OpenIZ.Core.Model.Interfaces;
using OpenIZ.Core.Services;
using OpenIZ.Persistence.Data.ADO.Data;
using OpenIZ.Persistence.Data.ADO.Data.Model;
using OpenIZ.Persistence.Data.ADO.Exceptions;
using OpenIZ.OrmLite;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using MARC.HI.EHRS.SVC.Core.Data;
using System.Diagnostics;
using System.Net.Sockets;

namespace OpenIZ.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Core persistence service which contains helpful functions
    /// </summary>
    public abstract class CorePersistenceService<TModel, TDomain, TQueryReturn> : AdoBasePersistenceService<TModel>
        where TModel : IdentifiedData, new()
        where TDomain : class, new()
    {

        // Query persistence
        protected MARC.HI.EHRS.SVC.Core.Services.IQueryPersistenceService m_queryPersistence = ApplicationContext.Current.GetService<MARC.HI.EHRS.SVC.Core.Services.IQueryPersistenceService>();

        /// <summary>
        /// Get the order by function
        /// </summary>
        protected virtual SqlStatement AppendOrderBy(SqlStatement rawQuery)
        {
            return rawQuery;
        }

        /// <summary>
        /// Maps the data to a model instance
        /// </summary>
        /// <returns>The model instance.</returns>
        /// <param name="dataInstance">Data instance.</param>
        public override TModel ToModelInstance(object dataInstance, DataContext context, IPrincipal principal)
        {
            var dInstance = (dataInstance as CompositeResult)?.Values.OfType<TDomain>().FirstOrDefault() ?? dataInstance as TDomain;
            var retVal = m_mapper.MapDomainInstance<TDomain, TModel>(dInstance);
            retVal.LoadAssociations(context, principal);
            this.m_tracer.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "Model instance {0} created", dataInstance);

            return retVal;
        }

        /// <summary>
		/// Performthe actual insert.
		/// </summary>
		/// <param name="context">Context.</param>
		/// <param name="data">Data.</param>
		public override TModel InsertInternal(DataContext context, TModel data, IPrincipal principal)
        {
            var domainObject = this.FromModelInstance(data, context, principal) as TDomain;

            domainObject = context.Insert<TDomain>(domainObject);

            if (domainObject is IDbIdentified)
                data.Key = (domainObject as IDbIdentified)?.Key;
            //data.CopyObjectData(this.ToModelInstance(domainObject, context, principal));
            //data.Key = domainObject.Key
            return data;
        }

        /// <summary>
        /// Perform the actual update.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public override TModel UpdateInternal(DataContext context, TModel data, IPrincipal principal)
        {
            // Sanity 
            if (data.Key == Guid.Empty)
                throw new AdoFormalConstraintException(AdoFormalConstraintType.NonIdentityUpdate);

            // Map and copy
            var newDomainObject = this.FromModelInstance(data, context, principal) as TDomain;
            context.Update<TDomain>(newDomainObject);
            return data;
        }

        /// <summary>
        /// Performs the actual obsoletion
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public override TModel ObsoleteInternal(DataContext context, TModel data, IPrincipal principal)
        {
            if (data.Key == Guid.Empty)
                throw new AdoFormalConstraintException(AdoFormalConstraintType.NonIdentityUpdate);

            context.Delete<TDomain>((TDomain)this.FromModelInstance(data, context, principal));

            return data;
        }

        /// <summary>
        /// Performs the actual query
        /// </summary>
        public override IEnumerable<TModel> QueryInternal(DataContext context, Expression<Func<TModel, bool>> query, Guid queryId, int offset, int? count, out int totalResults, IPrincipal principal, bool countResults = true)
        {
            int resultCount = 0;
            var results = this.DoQueryInternal(context, query, queryId, offset, count, out resultCount, countResults).ToList();
            totalResults = resultCount;

            if (!AdoPersistenceService.GetConfiguration().SingleThreadFetch)
            {
                return results.AsParallel().AsOrdered().Select(o =>
                {
                    var subContext = context;
                    var newSubContext = results.Count() > 1;
                    var idx = results.IndexOf(o);
                    try
                    {
                        if (newSubContext) subContext = subContext.OpenClonedContext();

                        if (o is Guid)
                            return this.Get(subContext, (Guid)o, principal);
                        else
                            return this.CacheConvert(o, subContext, principal);
                    }
                    catch (Exception e)
                    {
                        this.m_tracer.TraceEvent(TraceEventType.Error, 0, "Error performing sub-query: {0}", e);
                        throw;
                    }
                    finally
                    {
                        if (newSubContext)
                        {
                            foreach (var i in subContext.CacheOnCommit)
                                context.AddCacheCommit(i);
                            subContext.Dispose();
                        }
                    }
                });
            }
            else
                return results.Select(o =>
                {
                    if (o is Guid)
                        return this.Get(context, (Guid)o, principal);
                    else
                        return this.CacheConvert(o, context, principal);
                });
        }
        /// <summary>
        /// Perform the query 
        /// </summary>
        protected virtual IEnumerable<Object> DoQueryInternal(DataContext context, Expression<Func<TModel, bool>> query, Guid queryId, int offset, int? count, out int totalResults, bool includeCount = true)
        {
#if DEBUG
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif

            SqlStatement domainQuery = null;
            try
            {

                // Query has been registered?
                if (queryId != Guid.Empty && this.m_queryPersistence?.IsRegistered(queryId.ToString()) == true)
                    return this.GetStoredQueryResults(queryId, offset, count, out totalResults);

                // Is obsoletion time already specified?
                if (!query.ToString().Contains("ObsoletionTime") && typeof(BaseEntityData).IsAssignableFrom(typeof(TModel)))
                {
                    var obsoletionReference = Expression.MakeBinary(ExpressionType.Equal, Expression.MakeMemberAccess(query.Parameters[0], typeof(TModel).GetProperty(nameof(BaseEntityData.ObsoletionTime))), Expression.Constant(null));
                    query = Expression.Lambda<Func<TModel, bool>>(Expression.MakeBinary(ExpressionType.AndAlso, obsoletionReference, query.Body), query.Parameters);
                }

                // Domain query
                Type[] selectTypes = { typeof(TQueryReturn) };
                if (selectTypes[0].IsConstructedGenericType)
                    selectTypes = selectTypes[0].GenericTypeArguments;

                domainQuery = context.CreateSqlStatement<TDomain>().SelectFrom(selectTypes);
                var expression = m_mapper.MapModelExpression<TModel, TDomain>(query, false);
                if (expression != null)
                {
                    Type lastJoined = typeof(TDomain);
                    if (typeof(CompositeResult).IsAssignableFrom(typeof(TQueryReturn)))
                        foreach (var p in typeof(TQueryReturn).GenericTypeArguments.Select(o => AdoPersistenceService.GetMapper().MapModelType(o)))
                            if (p != typeof(TDomain))
                            {
                                // Find the FK to join
                                domainQuery.InnerJoin(lastJoined, p);
                                lastJoined = p;
                            }

                    domainQuery.Where<TDomain>(expression);
                }
                else
                {
                    m_tracer.TraceEvent(TraceEventType.Verbose, 0, "Will use slow query construction due to complex mapped fields");
                    domainQuery = AdoPersistenceService.GetQueryBuilder().CreateQuery(query);
                }

                // Count = 0 means we're not actually fetching anything so just hit the db
                if (count != 0)
                {

                    domainQuery = this.AppendOrderBy(domainQuery);
                    if (count == 1)
                        domainQuery.Limit(1);

                    // Only one is requested, or there is no future query coming back so no savings in querying the entire dataset
                    var retVal = this.DomainQueryInternal<TQueryReturn>(context, domainQuery).OfType<Object>();
                    if (includeCount)
                        totalResults = retVal.Count();
                    else
                        totalResults = 0;

                    // We have a query identifier and this is the first frame, freeze the query identifiers
                    if (queryId != Guid.Empty)
                        this.AddQueryResults<TQueryReturn>(context, query, queryId, offset, retVal, totalResults);

                    return retVal.Skip(offset).Take(count ?? 100);
                }
                else
                {
                    totalResults = context.Count(domainQuery);
                    return new List<Object>();
                }
            }
            catch (Exception ex)
            {
                if (domainQuery != null)
                    this.m_tracer.TraceEvent(TraceEventType.Error, ex.HResult, context.GetQueryLiteral(domainQuery.Build()));
                context.Dispose(); // No longer important

                throw;
            }
#if DEBUG
            finally
            {
                sw.Stop();
            }
#endif
        }

        /// <summary>
        /// Get stored query results
        /// </summary>
        protected IEnumerable<object> GetStoredQueryResults(Guid queryId, int offset, int? count, out int totalResults)
        {
            totalResults = (int)this.m_queryPersistence.QueryResultTotalQuantity(queryId.ToString());
            var keyResults = this.m_queryPersistence.GetQueryResults<Guid>(queryId.ToString(), offset, count.Value).Select(o=>o.Id);
            return keyResults.OfType<Object>();
        }

        /// <summary>
        /// Add query results
        /// </summary>
        protected void AddQueryResults<TKeySearch>(DataContext context, Expression<Func<TModel, bool>> query, Guid queryId, int offset, IEnumerable<Object> initialResults, int totalResults)
        {

            // Get PK Column to select keys
            ColumnMapping pkColumn = null;
            IEnumerable<Guid> keyResults = null;

            if (typeof(CompositeResult).IsAssignableFrom(typeof(TKeySearch)))
            {
                int keyObj = 0;
                foreach (var p in typeof(TKeySearch).GenericTypeArguments.Select(o => AdoPersistenceService.GetMapper().MapModelType(o)))
                {
                    if (!typeof(DbSubTable).IsAssignableFrom(p) && !typeof(IDbVersionedData).IsAssignableFrom(p))
                    {
                        pkColumn = TableMapping.Get(p).Columns.SingleOrDefault(o => o.IsPrimaryKey);
                        break;
                    }
                    keyObj++;
                }

                // Extract keys from composite
                Func<CompositeResult, Guid> selector = i => (Guid)pkColumn.SourceProperty.GetValue(i.Values[keyObj], null);
                keyResults = initialResults.OfType<CompositeResult>().Select(selector);

            }
            else
            {
                pkColumn = TableMapping.Get(typeof(TKeySearch)).Columns.SingleOrDefault(o => o.IsPrimaryKey);
                Func<IDbIdentified, Guid> selector = i => (Guid)pkColumn.SourceProperty.GetValue(i, null);
                keyResults = initialResults.OfType<IDbIdentified>().Select(selector);
            }

            this.m_queryPersistence?.RegisterQuerySet(queryId.ToString(), totalResults, keyResults.Select(o=>new Identifier<Guid>(o)).ToArray(), query);

            //int step = initialResults.Count();

            //// Build query for additional keys to query store if needed
            //if (initialResults.Count() < totalResults)
            //    ApplicationServiceContext.Current.GetService<IThreadPoolService>().QueueNonPooledWorkItem((parm) =>
            //    {
            //        var keyQuery = this.m_persistenceService.GetQueryBuilder().CreateQuery(query, orderBy, pkColumn);
            //        keyQuery = this.AppendOrderBy(keyQuery, orderBy);
            //        int ofs = offset == 0 ? step : 0;
            //        //while (ofs < totalResults)
            //        //{
            //        this.m_tracer.TraceVerbose("Hydrating query {0} ({1}..{2})", queryId, ofs, totalResults);
            //        var resultKeys = (parm as DataContext).Query<Guid>(keyQuery.Build().Offset(ofs));
            //        ofs = 0;
            //        while(ofs< totalResults) { 
            //            this.m_tracer.TraceVerbose("Registering results {0} ({1}..{2})", queryId, ofs, ofs + step);
            //            this.m_queryPersistence?.AddResults(queryId, resultKeys.Skip(ofs).Take(step).ToArray());
            //            ofs += step;
            //        }
            //    }, context.OpenClonedContext());

        }

        /// <summary>
        /// Perform a domain query
        /// </summary>
        protected IEnumerable<TResult> DomainQueryInternal<TResult>(DataContext context, SqlStatement domainQuery)
        {

            // Build and see if the query already exists on the stack???
            domainQuery = domainQuery.Build();

            var results = context.Query<TResult>(domainQuery);

            // Cache query result
            return results;

        }

        /// <summary>
        /// Build source query
        /// </summary>
        protected Expression<Func<TAssociation, bool>> BuildSourceQuery<TAssociation>(Guid sourceId) where TAssociation : ISimpleAssociation
        {
            return o => o.SourceEntityKey == sourceId;
        }

        /// <summary>
        /// Build source query
        /// </summary>
        protected Expression<Func<TAssociation, bool>> BuildSourceQuery<TAssociation>(Guid sourceId, decimal? versionSequenceId) where TAssociation : IVersionedAssociation
        {
            if (versionSequenceId == null)
                return o => o.SourceEntityKey == sourceId && o.ObsoleteVersionSequenceId == null;
            else
                return o => o.SourceEntityKey == sourceId && o.EffectiveVersionSequenceId <= versionSequenceId && (o.ObsoleteVersionSequenceId == null || o.ObsoleteVersionSequenceId > versionSequenceId);
        }

        /// <summary>
        /// Tru to load from cache
        /// </summary>
        protected virtual TModel CacheConvert(Object o, DataContext context, IPrincipal principal)
        {
            if (o == null) return null;

            var cacheService = new AdoPersistenceCache(context);

            var idData = (o as CompositeResult)?.Values.OfType<IDbIdentified>().FirstOrDefault() ?? o as IDbIdentified;
            var objData = (o as CompositeResult)?.Values.OfType<IDbBaseData>().FirstOrDefault() ?? o as IDbBaseData;
            if (objData?.ObsoletionTime != null || idData == null || idData.Key == Guid.Empty)
                return this.ToModelInstance(o, context, principal);
            else
            {
                var cacheItem = cacheService?.GetCacheItem<TModel>(idData?.Key ?? Guid.Empty);
                if (cacheItem != null)
                {
                    if (cacheItem.LoadState < context.LoadState)
                    {
                        cacheItem.LoadAssociations(context, principal);
                        cacheService?.Add(cacheItem);
                    }
                    return cacheItem;
                }
                else
                {
                    cacheItem = this.ToModelInstance(o, context, principal);
                    if (context.Transaction == null)
                        cacheService?.Add(cacheItem);
                }
                return cacheItem;
            }
        }

        /// <summary>
        /// Froms the model instance.
        /// </summary>
        /// <returns>The model instance.</returns>
        /// <param name="modelInstance">Model instance.</param>
        /// <param name="context">Context.</param>
        public override object FromModelInstance(TModel modelInstance, DataContext context, IPrincipal princpal)
        {
            return m_mapper.MapModelInstance<TModel, TDomain>(modelInstance);
        }

        /// <summary>
        /// Update associated items
        /// </summary>
        protected virtual void UpdateAssociatedItems<TAssociation, TDomainAssociation>(IEnumerable<TAssociation> storage, TModel source, DataContext context, IPrincipal principal)
            where TAssociation : IdentifiedData, ISimpleAssociation, new()
            where TDomainAssociation : DbAssociation, new()
        {
            var persistenceService = ApplicationContext.Current.GetService<IDataPersistenceService<TAssociation>>() as AdoBasePersistenceService<TAssociation>;
            if (persistenceService == null)
            {
                this.m_tracer.TraceEvent(System.Diagnostics.TraceEventType.Information, 0, "Missing persister for type {0}", typeof(TAssociation).Name);
                return;
            }
            // Ensure the source key is set
            foreach (var itm in storage)
                if (itm.SourceEntityKey == Guid.Empty ||
                    itm.SourceEntityKey == null)
                    itm.SourceEntityKey = source.Key;

            // Get existing
            var existing = context.Query<TDomainAssociation>(o => o.SourceKey == source.Key);
            // Remove old associations
            var obsoleteRecords = existing.Where(o => !storage.Any(ecn => ecn.Key == o.Key));
            foreach (var del in obsoleteRecords) // Obsolete records = delete as it is non-versioned association
                context.Delete(del);

            // Update those that need it
            var updateRecords = storage.Where(o => existing.Any(ecn => ecn.Key == o.Key && o.Key != Guid.Empty));
            foreach (var upd in updateRecords)
                persistenceService.UpdateInternal(context, upd, principal);

            // Insert those that do not exist
            var insertRecords = storage.Where(o => !existing.Any(ecn => ecn.Key == o.Key));
            foreach (var ins in insertRecords)
                persistenceService.InsertInternal(context, ins, principal);

        }


    }
}
