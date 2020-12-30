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
using OpenIZ.Core.Model.Entities;
using OpenIZ.Persistence.Data.ADO.Data.Model;
using System.Security.Principal;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Data;
using System;
using OpenIZ.Core.Services;
using System.Linq;
using OpenIZ.Persistence.Data.ADO.Data.Model.Acts;
using OpenIZ.Persistence.Data.ADO.Data;
using OpenIZ.Persistence.Data.ADO.Data.Model.Entities;
using OpenIZ.OrmLite;
using OpenIZ.Core.Model;
using OpenIZ.Core.Model.Constants;
using System.Linq.Expressions;
using System.Collections.Generic;
using MARC.HI.EHRS.SVC.Core.Services;
using OpenIZ.Persistence.Data.ADO.Data.Model.Concepts;
using OpenIZ.Persistence.Data.ADO.Data.Model.Security;
using OpenIZ.Persistence.Data.ADO.Data.Model.Roles;
using OpenIZ.Persistence.Data.ADO.Data.Model.Extensibility;
using OpenIZ.Persistence.Data.ADO.Data.Model.DataType;

namespace OpenIZ.Persistence.Data.ADO.Services.Persistence
{

    public class EntityDerivedPersistenceService<TModel, TData> : EntityDerivedPersistenceService<TModel, TData, CompositeResult<TData, DbEntityVersion, DbEntity>>
    where TModel : Core.Model.Entities.Entity, new()
    where TData : DbEntitySubTable, new()
    { }

    /// <summary>
    /// Entity derived persistence services
    /// </summary>
    public class EntityDerivedPersistenceService<TModel, TData, TQueryReturn> : SimpleVersionedEntityPersistenceService<TModel, TData, TQueryReturn, DbEntityVersion>
        where TModel : Core.Model.Entities.Entity, new()
        where TData : DbEntitySubTable, new()
        where TQueryReturn : CompositeResult
    {

        // Entity persister
        protected EntityPersistenceService m_entityPersister = new EntityPersistenceService();

        /// <summary>
        /// From model instance
        /// </summary>
        public override object FromModelInstance(TModel modelInstance, DataContext context, IPrincipal princpal)
        {
            var retVal = base.FromModelInstance(modelInstance, context, princpal);
            (retVal as DbEntitySubTable).ParentKey = modelInstance.VersionKey.Value;
            return retVal;
        }

        /// <summary>
        /// Entity model instance
        /// </summary>
        public override sealed TModel ToModelInstance(object dataInstance, DataContext context, IPrincipal principal)
        {
            return (TModel)this.m_entityPersister.ToModelInstance(dataInstance, context, principal);
        }

        /// <summary>
        /// Conversion based on type
        /// </summary>
        protected override TModel CacheConvert(object o, DataContext context, IPrincipal principal)
        {
            return (TModel)this.m_entityPersister.DoCacheConvert(o, context, principal);
        }

        /// <summary>
        /// Insert the specified TModel into the database
        /// </summary>
        public override TModel InsertInternal(DataContext context, TModel data, IPrincipal principal)
        {
            if (typeof(TModel).BaseType == typeof(Core.Model.Entities.Entity))
            {
                var inserted = this.m_entityPersister.InsertCoreProperties(context, data, principal);
                data.Key = inserted.Key;
                data.VersionKey = inserted.VersionKey;
            }
            return base.InsertInternal(context, data, principal);

        }

        /// <summary>
        /// If the linked act exists
        /// </summary>
        public override bool Exists(DataContext context, Guid key)
        {
            return this.m_entityPersister.Exists(context, key);
        }

        /// <summary>
        /// Update the specified TModel
        /// </summary>
        public override TModel UpdateInternal(DataContext context, TModel data, IPrincipal principal)
        {
            if (typeof(TModel).BaseType == typeof(Core.Model.Entities.Entity))
                this.m_entityPersister.UpdateCoreProperties(context, data, principal);
            return base.InsertInternal(context, data, principal);
            //return base.Update(context, data, principal);
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

            // Get most recent version
            if (uuid.VersionId == Guid.Empty)
                return base.Query(o => o.Key == uuid.Id && o.ObsoletionTime == null, 0, 1, principal, out tr).FirstOrDefault();
            else
                return base.Query(o => o.Key == uuid.Id && o.VersionKey == uuid.VersionId, 0, 1, principal, out tr).FirstOrDefault();
        }

        /// <summary>
        /// Obsolete the object
        /// </summary>
        public override TModel ObsoleteInternal(DataContext context, TModel data, IPrincipal principal)
        {
            if (typeof(TModel).BaseType == typeof(Core.Model.Entities.Entity))
                this.m_entityPersister.ObsoleteInternal(context, data, principal);
            return base.InsertInternal(context, data, principal);
        }


        /// <summary>
        /// Bulk obsolete
        /// </summary>
        protected override void BulkObsoleteInternal(DataContext context, IPrincipal principal, Guid[] keysToObsolete)
        {
            foreach (var k in keysToObsolete)
            {
                // Get the current version
                var currentVersion = context.SingleOrDefault<DbEntityVersion>(o => o.ObsoletionTime == null && o.Key == k);
                // Create a new version
                var newVersion = new DbEntityVersion();
                newVersion.CopyObjectData(currentVersion);

                // Create a new version which has a status of obsolete
                newVersion.StatusConceptKey = StatusKeys.Obsolete;
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
                newVersion = context.Insert(newVersion);

                // Finally, insert a new version of sub data
                var cversion = context.SingleOrDefault<TData>(o => o.ParentKey == currentVersion.VersionKey);
                var newSubVersion = new TData();
                newSubVersion.CopyObjectData(cversion);
                newSubVersion.ParentKey = newVersion.VersionKey;
                context.Insert(newSubVersion);

            }
        }

        /// <summary>
        /// Query for all keys
        /// </summary>
        protected override IEnumerable<Guid> QueryKeysInternal(DataContext context, Expression<Func<TModel, bool>> query, int offset, int? count, out int totalResults)
        {
            if (!query.ToString().Contains("ObsoletionTime") && !query.ToString().Contains("VersionKey"))
            {
                var obsoletionReference = Expression.MakeBinary(ExpressionType.Equal, Expression.MakeMemberAccess(query.Parameters[0], typeof(TModel).GetProperty(nameof(BaseEntityData.ObsoletionTime))), Expression.Constant(null));
                query = Expression.Lambda<Func<TModel, bool>>(Expression.MakeBinary(ExpressionType.AndAlso, obsoletionReference, query.Body), query.Parameters);
            }

            // Construct the SQL query
            var pk = TableMapping.Get(typeof(DbEntity)).Columns.SingleOrDefault(o => o.IsPrimaryKey);
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
        /// Purge the specified records (redirects to the entity persister)
        /// </summary>
        public override void Purge(TransactionMode transactionMode, IPrincipal principal, params Guid[] keysToPurge)
        {
            this.m_entityPersister.Purge(transactionMode, principal, keysToPurge);
        }

        /// <summary>
        /// Bulk purge
        /// </summary>
        protected override void BulkPurgeInternal(DataContext connection, IPrincipal principal, Guid[] keysToPurge)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Copy specified keys
        /// </summary>
        public override void Copy(Guid[] keysToCopy, DataContext fromContext, DataContext toContext)
        {
            this.m_entityPersister.Copy(keysToCopy, fromContext, toContext);
        }

      
    }
}