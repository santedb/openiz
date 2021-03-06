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
using OpenIZ.Core.Model;
using OpenIZ.Persistence.Data.ADO.Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using OpenIZ.Core.Model.DataTypes;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using System.ComponentModel;
using MARC.HI.EHRS.SVC.Core.Data;
using OpenIZ.Core.Services;
using OpenIZ.Core;
using System.Reflection;
using System.Linq.Expressions;
using OpenIZ.Persistence.Data.ADO.Data;
using OpenIZ.Persistence.Data.ADO.Exceptions;
using OpenIZ.OrmLite;
using MARC.HI.EHRS.SVC.Core.Event;
using System.Diagnostics;
using OpenIZ.Core.Exceptions;
using OpenIZ.Persistence.Data.ADO.Data.Model.Concepts;
using OpenIZ.Persistence.Data.ADO.Data.Model.Acts;
using OpenIZ.Persistence.Data.ADO.Data.Model.Entities;
using OpenIZ.Core.Model.Acts;
using OpenIZ.Core.Model.Entities;
using OpenIZ.Core.Diagnostics;
using OpenIZ.Core.Model.Constants;

namespace OpenIZ.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Versioned domain data
    /// </summary>
    public abstract class VersionedDataPersistenceService<TModel, TDomain, TDomainKey> : BaseDataPersistenceService<TModel, TDomain, CompositeResult<TDomain, TDomainKey>>
        where TDomain : class, IDbVersionedData, new()
        where TModel : VersionedEntityData<TModel>, new()
        where TDomainKey : IDbIdentified, new()
    {

        /// <summary>
        /// Return true if the specified object exists
        /// </summary>
        public override bool Exists(DataContext context, Guid key)
        {
            return context.Any<TDomainKey>(o => o.Key == key);
        }

        /// <summary>
        /// Insert the data
        /// </summary>
        public override TModel InsertInternal(DataContext context, TModel data, IPrincipal principal)
        {
            // Ensure exists
            data.CreatedBy?.EnsureExists(context, principal);
            data.CreatedByKey = data.CreatedBy?.Key ?? data.CreatedByKey;

            // first we map the TDataKey entity
            var nonVersionedPortion = m_mapper.MapModelInstance<TModel, TDomainKey>(data);

            // Domain object
            var domainObject = this.FromModelInstance(data, context, principal) as TDomain;

            // First we must assign non versioned portion data
            if (nonVersionedPortion.Key == Guid.Empty &&
                domainObject.Key != Guid.Empty)
                nonVersionedPortion.Key = domainObject.Key;

            if (nonVersionedPortion.Key == null ||
                nonVersionedPortion.Key == Guid.Empty)
            {
                data.Key = Guid.NewGuid();
                domainObject.Key = nonVersionedPortion.Key = data.Key.Value;
            }
            if (domainObject.VersionKey == null ||
                domainObject.VersionKey == Guid.Empty)
            {
                data.VersionKey = Guid.NewGuid();
                domainObject.VersionKey = data.VersionKey.Value;
            }

            // Now we want to insert the non versioned portion first
            nonVersionedPortion = context.Insert(nonVersionedPortion);

            // Ensure created by exists
            data.CreatedByKey = domainObject.CreatedByKey = domainObject.CreatedByKey == Guid.Empty ? principal.GetUserKey(context).Value : domainObject.CreatedByKey;

            if (data.CreationTime == DateTimeOffset.MinValue || data.CreationTime.Year < 100)
                data.CreationTime = DateTimeOffset.Now;
            domainObject.CreationTime = data.CreationTime;
            domainObject.VersionSequenceId = null;
            domainObject.ObsoletionTime = null;
            domainObject.ObsoletedByKey = null;
            if (m_configuration.AutoUpdateExisting &&
                context.Any<TDomain>(o => o.Key == domainObject.Key))
            {
                this.m_tracer.TraceWarning("INSERT for {0} key={1} was changed to UPDATE as item already exists", domainObject.GetType().Name, domainObject.Key);
                domainObject = context.Update(domainObject);
            }
            else
                domainObject = context.Insert(domainObject);

            data.VersionSequence = domainObject.VersionSequenceId;
            data.VersionKey = domainObject.VersionKey;
            data.Key = domainObject.Key;
            data.CreationTime = (DateTimeOffset)domainObject.CreationTime;
            return data;

        }

        /// <summary>
        /// Update the data with new version information
        /// </summary>
        public override TModel UpdateInternal(DataContext context, TModel data, IPrincipal principal)
        {
            if (data.Key == Guid.Empty)
                throw new AdoFormalConstraintException(AdoFormalConstraintType.NonIdentityUpdate);

            data.CreatedBy.EnsureExists(context, principal);
            data.CreatedByKey = data.CreatedBy?.Key ?? data.CreatedByKey;
            if (data.CreatedByKey == Guid.Empty) // HACK: For some reason the UUID For created by is null
                data.CreatedByKey = null;

            // This is technically an insert and not an update
            SqlStatement currentVersionQuery = context.CreateSqlStatement<TDomain>().SelectFrom()
                .Where(o => o.Key == data.Key && !o.ObsoletionTime.HasValue)
                .OrderBy<TDomain>(o => o.VersionSequenceId, Core.Model.Map.SortOrderType.OrderByDescending);

            var existingObject = context.FirstOrDefault<TDomain>(currentVersionQuery); // Get the last version (current)
            var nonVersionedObect = context.FirstOrDefault<TDomainKey>(o => o.Key == data.Key);

            if (existingObject == null)
            {
                if(nonVersionedObect != null) // There is just no active version - so - activate the last one
                {
                    currentVersionQuery = context.CreateSqlStatement<TDomain>().SelectFrom()
                        .Where(o => o.Key == data.Key)
                        .OrderBy<TDomain>(o => o.VersionSequenceId, Core.Model.Map.SortOrderType.OrderByDescending);
                    existingObject = context.FirstOrDefault<TDomain>(currentVersionQuery); // Get the last version (current)
                }
                else
                    throw new KeyNotFoundException(data.Key.ToString());
            }
            else if ((existingObject as IDbReadonly)?.IsReadonly == true ||
                (nonVersionedObect as IDbReadonly)?.IsReadonly == true)
                throw new AdoFormalConstraintException(AdoFormalConstraintType.UpdatedReadonlyObject);

            // Map existing
            var storageInstance = this.FromModelInstance(data, context, principal);

            // Create a new version
            var user = principal.GetUserKey(context);
            var newEntityVersion = new TDomain();
            newEntityVersion.CopyObjectData(storageInstance);

            // Client did not change on update, so we need to update!!!
            if (!data.VersionKey.HasValue ||
               data.VersionKey.Value == existingObject.VersionKey ||
               context.Any<TDomain>(o => o.VersionKey == data.VersionKey))
                data.VersionKey = newEntityVersion.VersionKey = Guid.NewGuid();

            data.VersionSequence = newEntityVersion.VersionSequenceId = null;
            newEntityVersion.Key = data.Key.Value;
            data.PreviousVersionKey = newEntityVersion.ReplacesVersionKey = existingObject.VersionKey;
            data.CreatedByKey = newEntityVersion.CreatedByKey = data.CreatedByKey ?? user.Value;
            // Obsolete the old version 
            existingObject.ObsoletedByKey = data.CreatedByKey ?? user;
            existingObject.ObsoletionTime = DateTimeOffset.Now;
            newEntityVersion.CreationTime = DateTimeOffset.Now;

            context.Update(existingObject);

            // There must always be an active version
            newEntityVersion.ObsoletedByKey = null;
            newEntityVersion.ObsoletionTime = null;
            newEntityVersion = context.Insert<TDomain>(newEntityVersion);
            nonVersionedObect = context.Update<TDomainKey>(nonVersionedObect);

            // Pull database generated fields
            data.VersionSequence = newEntityVersion.VersionSequenceId;
            data.CreationTime = newEntityVersion.CreationTime;

            return data;
            //return base.Update(context, data, principal);
        }


        /// <summary>
        /// Order by
        /// </summary>
        /// <param name="rawQuery"></param>
        /// <returns></returns>
        protected override SqlStatement AppendOrderBy(SqlStatement rawQuery)
        {
            return rawQuery.OrderBy<TDomain>(o => o.VersionSequenceId, Core.Model.Map.SortOrderType.OrderByDescending);
        }


        /// <summary>
        /// Query internal
        /// </summary>
        protected override IEnumerable<Object> DoQueryInternal(DataContext context, Expression<Func<TModel, bool>> query, Guid queryId, int offset, int? count, out int totalResults, bool countResults = true)
        {
            // Is obsoletion time already specified?
            if (!query.ToString().Contains("ObsoletionTime"))
            {
                var obsoletionReference = Expression.MakeBinary(ExpressionType.Equal, Expression.MakeMemberAccess(query.Parameters[0], typeof(TModel).GetProperty(nameof(BaseEntityData.ObsoletionTime))), Expression.Constant(null));
                query = Expression.Lambda<Func<TModel, bool>>(Expression.MakeBinary(ExpressionType.AndAlso, obsoletionReference, query.Body), query.Parameters);
            }


            // Query has been registered?
            if (queryId != Guid.Empty && this.m_queryPersistence?.IsRegistered(queryId.ToString()) == true)
                return this.GetStoredQueryResults(queryId, offset, count, out totalResults);

            SqlStatement domainQuery = null;
            var expr = m_mapper.MapModelExpression<TModel, TDomain>(query, false);
            if (expr != null)
                domainQuery = context.CreateSqlStatement<TDomain>().SelectFrom(typeof(TDomain), typeof(TDomainKey))
                    .InnerJoin<TDomain, TDomainKey>(o => o.Key, o => o.Key)
                    .Where<TDomain>(expr).Build();
            else
                domainQuery = AdoPersistenceService.GetQueryBuilder().CreateQuery(query).Build();


            domainQuery = this.AppendOrderBy(domainQuery);

            // Only perform count
            if (count == 0)
            {
                totalResults = context.Count(domainQuery);
                return new List<CompositeResult<TDomain, TDomainKey>>();
            }
            else
            {
                
                var retVal = context.Query<CompositeResult<TDomain, TDomainKey>>(domainQuery);

                // Counts
                if (queryId != Guid.Empty && ApplicationContext.Current.GetService<MARC.HI.EHRS.SVC.Core.Services.IQueryPersistenceService>() != null)
                {
                    var keys = retVal.Keys<Guid>().ToArray();
                    totalResults = keys.Length;
                    this.AddQueryResults(context, query, queryId, offset, keys, totalResults);
                    if (totalResults == 0)
                        return new List<Object>();
                }
                else if (count.HasValue && countResults && !m_configuration.UseFuzzyTotals)
                {
                    totalResults = retVal.Count();
                    if (totalResults == 0)
                        return new List<Object>();
                }
                else
                    totalResults = 0;

                // Fuzzy totals - This will only fetch COUNT + 1 as the total results
                if (count.HasValue)
                {
                    if (m_configuration.UseFuzzyTotals && totalResults == 0)
                    {
                        var fuzzResults = retVal.Skip(offset).Take(count.Value + 1).OfType<Object>().ToList();
                        totalResults = fuzzResults.Count();
                        return fuzzResults.Take(count.Value);
                    }
                    else // We already counted as part of the queryId so no need to take + 1
                        return retVal.Skip(offset).Take(count.Value).OfType<Object>();
                }
                else
                    return retVal.Skip(offset).OfType<Object>();

            }


        }

        /// <summary>
        /// Perform a version aware get
        /// </summary>
        internal override TModel Get(DataContext context, Guid key, IPrincipal principal)
        {
            // Attempt to get a cahce item
            var cacheService = new AdoPersistenceCache(context);
            var retVal = cacheService.GetCacheItem<TModel>(key);
            if (retVal != null)
            {
                this.m_tracer.TraceVerbose("Object {0} found in cache", retVal);
                return retVal;
            }
            else
            {
                this.m_tracer.TraceVerbose("Object {0}({1}) not found in cache will load", typeof(TModel).FullName, key);

                var domainQuery = context.CreateSqlStatement<TDomain>().SelectFrom()
                    .InnerJoin<TDomain, TDomainKey>(o => o.Key, o => o.Key)
                    .Where<TDomain>(o => o.Key == key && o.ObsoletionTime == null)
                    .OrderBy<TDomain>(o => o.VersionSequenceId, Core.Model.Map.SortOrderType.OrderByDescending);
                return this.CacheConvert(context.FirstOrDefault<CompositeResult<TDomain, TDomainKey>>(domainQuery), context, principal);
            }
        }

        /// <summary>
        /// Gets the specified object
        /// </summary>
        public override TModel Get<TIdentifier>(MARC.HI.EHRS.SVC.Core.Data.Identifier<TIdentifier> containerId, IPrincipal principal, bool loadFast)
        {
            var tr = 0;
            var uuid = containerId as Identifier<Guid>;

            if (uuid.Id != Guid.Empty)
            {

                var cacheItem = ApplicationContext.Current.GetService<IDataCachingService>()?.GetCacheItem<TModel>(uuid.Id) as TModel;
                if (cacheItem != null && (cacheItem.VersionKey.HasValue && uuid.VersionId == cacheItem.VersionKey.Value || uuid.VersionId == Guid.Empty) &&
                    (loadFast && cacheItem.LoadState >= LoadState.PartialLoad || !loadFast && cacheItem.LoadState == LoadState.FullLoad))
                    return cacheItem;
            }

#if DEBUG
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif

            PreRetrievalEventArgs preArgs = new PreRetrievalEventArgs(containerId, principal);
            this.FireRetrieving(preArgs);
            if (preArgs.Cancel)
            {
                this.m_tracer.TraceEvent(TraceEventType.Warning, 0, "Pre-Event handler indicates abort retrieve {0}", containerId.Id);
                return null;
            }

            // Query object
            using (var connection = m_configuration.Provider.GetReadonlyConnection())
                try
                {
                    connection.Open();
                    this.m_tracer.TraceEvent(TraceEventType.Verbose, 0, "GET {0}", containerId);

                    TModel retVal = null;
                    if (loadFast)
                        connection.LoadState = LoadState.PartialLoad;
                    else
                        connection.LoadState = LoadState.FullLoad;

                    // Get most recent version
                    if (uuid.VersionId == Guid.Empty)
                        retVal = this.Get(connection, uuid.Id, principal);
                    else
                        retVal = this.CacheConvert(this.DoQueryInternal(connection, o => o.Key == uuid.Id && o.VersionKey == uuid.VersionId && o.ObsoletionTime == null || o.ObsoletionTime != null, Guid.Empty, 0, 1, out tr).FirstOrDefault(), connection, principal);

                    var postData = new PostRetrievalEventArgs<TModel>(retVal, principal);
                    this.FireRetrieved(postData);

                    // Add to cache
                    foreach (var d in connection.CacheOnCommit)
                        ApplicationContext.Current.GetService<IDataCachingService>().Add(d);

                    return retVal;

                }
                catch (NotSupportedException e)
                {
                    throw new DataPersistenceException("Cannot perform LINQ query", e);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceEvent(TraceEventType.Error, 0, "Error : {0}", e);
                    throw;
                }
                finally
                {
#if DEBUG
                    sw.Stop();
                    this.m_tracer.TraceEvent(TraceEventType.Verbose, 0, "Retrieve took {0} ms", sw.ElapsedMilliseconds);
#endif
                }

        }

        /// <summary>
        /// Query keys for versioned objects 
        /// </summary>
        /// <remarks>This redirects the query from the primary key (on TDomain) into the primary key on the base object</remarks>
        protected override IEnumerable<Guid> QueryKeysInternal(DataContext context, Expression<Func<TModel, bool>> query, int offset, int? count, out int totalResults)
        {
            if (!query.ToString().Contains("ObsoletionTime") && !query.ToString().Contains("VersionKey"))
            {
                var obsoletionReference = Expression.MakeBinary(ExpressionType.Equal, Expression.MakeMemberAccess(query.Parameters[0], typeof(TModel).GetProperty(nameof(BaseEntityData.ObsoletionTime))), Expression.Constant(null));
                query = Expression.Lambda<Func<TModel, bool>>(Expression.MakeBinary(ExpressionType.AndAlso, obsoletionReference, query.Body), query.Parameters);
            }

            // Construct the SQL query
            var pk = TableMapping.Get(typeof(TDomainKey)).Columns.SingleOrDefault(o => o.IsPrimaryKey);
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
        /// Update versioned association items
        /// </summary>
        internal virtual void UpdateVersionedAssociatedItems<TAssociation, TDomainAssociation>(IEnumerable<TAssociation> storage, TModel source, DataContext context, IPrincipal principal)
            where TAssociation : VersionedAssociation<TModel>, new()
            where TDomainAssociation : class, IDbVersionedAssociation, IDbIdentified, new()
        {
            var persistenceService = ApplicationContext.Current.GetService<IDataPersistenceService<TAssociation>>() as AdoBasePersistenceService<TAssociation>;
            if (persistenceService == null)
            {
                this.m_tracer.TraceEvent(System.Diagnostics.TraceEventType.Information, 0, "Missing persister for type {0}", typeof(TAssociation).Name);
                return;
            }

            Dictionary<Guid, Decimal> sourceVersionMaps = new Dictionary<Guid, decimal>();

            // Ensure the source key is set
            foreach (var itm in storage)
                if (itm.SourceEntityKey.GetValueOrDefault() == Guid.Empty ||
                    itm.SourceEntityKey == null)
                    itm.SourceEntityKey = source.Key;
                else if (itm.SourceEntityKey != source.Key && !sourceVersionMaps.ContainsKey(itm.SourceEntityKey ?? Guid.Empty)) // The source comes from somewhere else
                {

                    SqlStatement versionQuery = null;
                    // Get the current tuple 
                    IDbVersionedData currentVersion = null;

                    // We need to figure out what the current version of the source item is ... 
                    // Since this is a versioned association an a versioned association only exists between Concept, Act, or Entity
                    if (itm is VersionedAssociation<Concept>)
                    {
                        versionQuery = context.CreateSqlStatement<DbConceptVersion>().SelectFrom().Where(o => o.VersionKey == itm.SourceEntityKey && !o.ObsoletionTime.HasValue).OrderBy<DbConceptVersion>(o => o.VersionSequenceId, Core.Model.Map.SortOrderType.OrderByDescending);
                        currentVersion = context.FirstOrDefault<DbConceptVersion>(versionQuery);
                    }
                    else if (itm is VersionedAssociation<Act>)
                    {
                        versionQuery = context.CreateSqlStatement<DbActVersion>().SelectFrom().Where(o => o.Key == itm.SourceEntityKey && !o.ObsoletionTime.HasValue).OrderBy<DbActVersion>(o => o.VersionSequenceId, Core.Model.Map.SortOrderType.OrderByDescending);
                        currentVersion = context.FirstOrDefault<DbActVersion>(versionQuery);
                    }
                    else if (itm is VersionedAssociation<Entity>)
                    {
                        versionQuery = context.CreateSqlStatement<DbEntityVersion>().SelectFrom().Where(o => o.Key == itm.SourceEntityKey && !o.ObsoletionTime.HasValue).OrderBy<DbEntityVersion>(o => o.VersionSequenceId, Core.Model.Map.SortOrderType.OrderByDescending);
                        currentVersion = context.FirstOrDefault<DbEntityVersion>(versionQuery);
                    }

                    if (currentVersion != null)
                        sourceVersionMaps.Add(itm.SourceEntityKey.Value, currentVersion.VersionSequenceId.Value);
                }

            // Get existing
            // TODO: What happens which this is reverse?
            var existing = context.Query<TDomainAssociation>(o => o.SourceKey == source.Key).ToArray();

            // Remove old
            var obsoleteRecords = existing.Where(o => !storage.Any(ecn => ecn.Key == o.Key));
            foreach (var del in obsoleteRecords)
            {
                decimal obsVersion = 0;
                if (!sourceVersionMaps.TryGetValue(del.SourceKey, out obsVersion))
                    obsVersion = source.VersionSequence.GetValueOrDefault();

#if DEBUG
                this.m_tracer.TraceInformation("----- OBSOLETING {0} {1} ---- ", del.GetType().Name, del.Key);
#endif
                del.ObsoleteVersionSequenceId = obsVersion;
                context.Update<TDomainAssociation>(del);
            }

            // Update those that need it
            var updateRecords = storage.Select(o => new { store = o, existing = existing.FirstOrDefault(ecn => ecn.Key == o.Key && o.Key != Guid.Empty && o != ecn) }).Where(o => o.existing != null);
            foreach (var upd in updateRecords)
            {
                // Update by key, these lines make no sense we just update the existing versioned association
                //upd.store.EffectiveVersionSequenceId = upd.existing.EffectiveVersionSequenceId;
                //upd.store.ObsoleteVersionSequenceId = upd.existing.EffectiveVersionSequenceId;
                persistenceService.UpdateInternal(context, upd.store as TAssociation, principal);
            }

            // Insert those that do not exist
            var insertRecords = storage.Where(o => !existing.Any(ecn => ecn.Key == o.Key));
            foreach (var ins in insertRecords)
            {
                decimal eftVersion = 0;
                if (!sourceVersionMaps.TryGetValue(ins.SourceEntityKey.Value, out eftVersion))
                    eftVersion = source.VersionSequence.GetValueOrDefault();
                ins.EffectiveVersionSequenceId = eftVersion;

                if (ins.Key.HasValue && persistenceService.Get(context, ins.Key.Value, principal) != null)
                    persistenceService.UpdateInternal(context, ins, principal);
                else
                    persistenceService.InsertInternal(context, ins, principal);
            }
        }

        /// <summary>
        /// Obsolete the specified keys
        /// </summary>
        protected sealed override void BulkObsoleteInternal(DataContext context, IPrincipal principal, Guid[] keysToObsolete)
        {
            foreach (var k in keysToObsolete)
            {

                // Get the current version
                var currentVersion = context.SingleOrDefault<TDomain>(o => o.ObsoletionTime == null && o.Key == k);
                // Create a new version
                var newVersion = new TDomain();
                newVersion.CopyObjectData(currentVersion);

                // Create a new version which has a status of obsolete
                if (newVersion is IDbHasStatus status)
                {
                    status.StatusConceptKey = StatusKeys.Obsolete;
                    // Update the old version
                    currentVersion.ObsoletedByKey = principal.GetUserKey(context);
                    currentVersion.ObsoletionTime = DateTimeOffset.Now;
                    context.Update(currentVersion);
                    // Provenance data
                    newVersion.VersionSequenceId = null;
                    newVersion.ReplacesVersionKey = currentVersion.VersionKey;
                    newVersion.CreatedByKey = principal.GetUserKey(context).GetValueOrDefault();
                    newVersion.CreationTime = DateTimeOffset.Now;
                    newVersion.VersionKey = Guid.Empty;
                    context.Insert(newVersion);

                }
                else // Just remove the version
                {
                    // Update the old version
                    currentVersion.ObsoletedByKey = principal.GetUserKey(context);
                    currentVersion.ObsoletionTime = DateTimeOffset.Now;
                    context.Update(currentVersion);
                }
            }
        }
    }
}
