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
using MARC.HI.EHRS.SVC.Core.Data;
using OpenIZ.Core.Model.Acts;
using OpenIZ.Core.Services;
using OpenIZ.OrmLite;
using OpenIZ.Persistence.Data.ADO.Data;
using OpenIZ.Persistence.Data.ADO.Data.Model;
using OpenIZ.Persistence.Data.ADO.Data.Model.Acts;
using OpenIZ.OrmLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using MARC.HI.EHRS.SVC.Core.Services;
using OpenIZ.Core.Model;
using OpenIZ.Core.Model.Constants;

namespace OpenIZ.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Represents a persistence service which is derived from an act
    /// </summary>
    public abstract class ActDerivedPersistenceService<TModel, TData> : ActDerivedPersistenceService<TModel, TData, CompositeResult<TData, DbActVersion, DbAct>>
        where TModel : Core.Model.Acts.Act, new()
        where TData : DbActSubTable, new()
    { }

    /// <summary>
    /// Represents a persistence service which is derived from an act
    /// </summary>
    public abstract class ActDerivedPersistenceService<TModel, TData, TQueryReturn> : SimpleVersionedEntityPersistenceService<TModel, TData, TQueryReturn, DbActVersion>
        where TModel : Core.Model.Acts.Act, new()
        where TData : DbActSubTable, new()
        where TQueryReturn : CompositeResult
    {
        // act persister
        protected ActPersistenceService m_actPersister = new ActPersistenceService();


        /// <summary>
        /// If the linked act exists
        /// </summary>
        public override bool Exists(DataContext context, Guid key)
        {
            return this.m_actPersister.Exists(context, key);
        }

        /// <summary>
        /// From model instance
        /// </summary>
        public override object FromModelInstance(TModel modelInstance, DataContext context, IPrincipal princpal)
        {
            var retVal = base.FromModelInstance(modelInstance, context, princpal);
            (retVal as DbActSubTable).ParentKey = modelInstance.VersionKey.Value;
            return retVal;
        }

        /// <summary>
        /// Entity model instance
        /// </summary>
        public override sealed TModel ToModelInstance(object dataInstance, DataContext context, IPrincipal principal)
        {
            return (TModel)this.m_actPersister.ToModelInstance(dataInstance, context, principal);
        }

        /// <summary>
        /// Insert the specified TModel into the database
        /// </summary>
        public override TModel InsertInternal(DataContext context, TModel data, IPrincipal principal)
        {
            if (typeof(TModel).BaseType == typeof(Act))
            {
                var inserted = this.m_actPersister.InsertCoreProperties(context, data, principal);
                data.Key = inserted.Key;
            }
            return base.InsertInternal(context, data, principal);
        }

        /// <summary>
        /// Update the specified TModel
        /// </summary>
        public override TModel UpdateInternal(DataContext context, TModel data, IPrincipal principal)
        {
            if (typeof(TModel).BaseType == typeof(Act))
                this.m_actPersister.UpdateCoreProperties(context, data, principal);
            return base.InsertInternal(context, data, principal);
        }

        /// <summary>
        /// Obsolete the object
        /// </summary>
        public override TModel ObsoleteInternal(DataContext context, TModel data, IPrincipal principal)
        {
            var retVal = this.m_actPersister.ObsoleteInternal(context, data, principal);
            return base.InsertInternal(context, data, principal);
        }


        /// <summary>
        /// Purge the specified records (redirects to the act persister)
        /// </summary>
        public override void Purge(TransactionMode transactionMode, IPrincipal principal, params Guid[] keysToPurge)
        {
            this.m_actPersister.Purge(transactionMode, principal, keysToPurge);
        }

        /// <summary>
        /// Bulk obsolete 
        /// </summary>
        protected override void BulkObsoleteInternal(DataContext context, IPrincipal principal, Guid[] keysToObsolete)
        {
            foreach (var k in keysToObsolete)
            {
                // Get the current version
                var currentVersion = context.SingleOrDefault<DbActVersion>(o => o.ObsoletionTime == null && o.Key == k);
                // Create a new version
                var newVersion = new DbActVersion();
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
        /// Bulk purge
        /// </summary>
        protected override void BulkPurgeInternal(DataContext connection, IPrincipal principal, Guid[] keysToPurge)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Copy keys
        /// </summary>
        public override void Copy(Guid[] keysToCopy, DataContext fromContext, DataContext toContext)
        {
            this.m_actPersister.Copy(keysToCopy, fromContext, toContext);
        }

        /// <summary>
        /// Query keys 
        /// </summary>
        protected override IEnumerable<Guid> QueryKeysInternal(DataContext context, Expression<Func<TModel, bool>> query, int offset, int? count, out int totalResults)
        {
            if (!query.ToString().Contains("ObsoletionTime") && !query.ToString().Contains("VersionKey"))
            {
                var obsoletionReference = Expression.MakeBinary(ExpressionType.Equal, Expression.MakeMemberAccess(query.Parameters[0], typeof(TModel).GetProperty(nameof(BaseEntityData.ObsoletionTime))), Expression.Constant(null));
                query = Expression.Lambda<Func<TModel, bool>>(Expression.MakeBinary(ExpressionType.AndAlso, obsoletionReference, query.Body), query.Parameters);
            }

            // Construct the SQL query
            var pk = TableMapping.Get(typeof(DbAct)).Columns.SingleOrDefault(o => o.IsPrimaryKey);
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
    }
}
