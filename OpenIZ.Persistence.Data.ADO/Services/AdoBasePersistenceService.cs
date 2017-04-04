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
 * Date: 2016-8-2
 */
using System;
using System.Linq;
using OpenIZ.Core.Model.Map;
using System.Reflection;
using OpenIZ.Core.Model;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using OpenIZ.Core.Exceptions;
using OpenIZ.Core.Model.Query;
using MARC.HI.EHRS.SVC.Core.Services;
using System.Diagnostics;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Event;
using System.Security.Principal;
using System.Data.SqlClient;
using OpenIZ.Core.Security;
using MARC.HI.EHRS.SVC.Core.Data;
using System.Data.Common;
using OpenIZ.Core.Services;
using OpenIZ.Persistence.Data.ADO.Configuration;
using OpenIZ.Persistence.Data.ADO.Data.Model;
using System.Data;
using OpenIZ.Persistence.Data.ADO.Data.Model.Acts;
using OpenIZ.Persistence.Data.ADO.Data.Model.DataType;
using OpenIZ.Core.Model.Constants;
using OpenIZ.Persistence.Data.ADO.Data;
using OpenIZ.OrmLite;

namespace OpenIZ.Persistence.Data.ADO.Services
{
    /// <summary>
    /// Represents a data persistence service which stores data in the local SQLite data store
    /// </summary>
    public abstract class AdoBasePersistenceService<TData> : IDataPersistenceService<TData>, IAdoPersistenceService where TData : IdentifiedData
    {

        // Lock for editing 
        protected object m_synkLock = new object();

        // Get tracer
        protected TraceSource m_tracer = new TraceSource(AdoDataConstants.TraceSourceName);

        // Configuration
        protected static AdoConfiguration m_configuration = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection(AdoDataConstants.ConfigurationSectionName) as AdoConfiguration;

        // Mapper
        protected static ModelMapper m_mapper = AdoPersistenceService.GetMapper();

        public event EventHandler<PrePersistenceEventArgs<TData>> Inserting;
        public event EventHandler<PostPersistenceEventArgs<TData>> Inserted;
        public event EventHandler<PrePersistenceEventArgs<TData>> Updating;
        public event EventHandler<PostPersistenceEventArgs<TData>> Updated;
        public event EventHandler<PrePersistenceEventArgs<TData>> Obsoleting;
        public event EventHandler<PostPersistenceEventArgs<TData>> Obsoleted;
        public event EventHandler<PreRetrievalEventArgs> Retrieving;
        public event EventHandler<PostRetrievalEventArgs<TData>> Retrieved;
        public event EventHandler<PreQueryEventArgs<TData>> Querying;
        public event EventHandler<PostQueryEventArgs<TData>> Queried;

        /// <summary>
        /// Maps the data to a model instance
        /// </summary>
        /// <returns>The model instance.</returns>
        /// <param name="dataInstance">Data instance.</param>
        public abstract TData ToModelInstance(Object dataInstance, DataContext context, IPrincipal principal);

        /// <summary>
        /// Froms the model instance.
        /// </summary>
        /// <returns>The model instance.</returns>
        /// <param name="modelInstance">Model instance.</param>
        public abstract Object FromModelInstance(TData modelInstance, DataContext context, IPrincipal principal);

        /// <summary>
        /// Performthe actual insert.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public abstract TData InsertInternal(DataContext context, TData data, IPrincipal principal);

        /// <summary>
        /// Perform the actual update.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public abstract TData UpdateInternal(DataContext context, TData data, IPrincipal principal);

        /// <summary>
        /// Performs the actual obsoletion
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public abstract TData ObsoleteInternal(DataContext context, TData data, IPrincipal principal);

        /// <summary>
        /// Performs the actual query
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="query">Query.</param>
        public abstract IEnumerable<TData> QueryInternal(DataContext context, Expression<Func<TData, bool>> query, int offset, int? count, out int totalResults, IPrincipal principal, bool countResults = true);

        /// <summary>
        /// Get the specified key.
        /// </summary>
        /// <param name="key">Key.</param>
        internal virtual TData Get(DataContext context, Guid key, IPrincipal principal)
        {
            int tr = 0;
            return this.QueryInternal(context, o => o.Key == key, 0, 1, out tr, principal)?.FirstOrDefault();
        }

        /// <summary>
        /// Inserts the specified data
        /// </summary>
        public TData Insert(TData data, IPrincipal principal, TransactionMode mode)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            PrePersistenceEventArgs<TData> preArgs = new PrePersistenceEventArgs<TData>(data, principal);
            this.Inserting?.Invoke(this, preArgs);
            if (preArgs.Cancel)
            {
                this.m_tracer.TraceEvent(TraceEventType.Warning, 0, "Pre-Event handler indicates abort insert for {0}", data);
                return data;
            }

            // Persist object
            using (var connection = m_configuration.Provider.GetWriteConnection())
            {
                connection.Open();
                using (IDbTransaction tx = connection.BeginTransaction())
                    try
                    {

                        // Disable inserting duplicate classified objects
                        var existing =data.TryGetExisting(connection, principal);
                        if (existing != null)
                        {
                            if (m_configuration.AutoUpdateExisting)
                            {
                                this.m_tracer.TraceEvent(TraceEventType.Warning, 0, "INSERT WOULD RESULT IN DUPLICATE CLASSIFIER: UPDATING INSTEAD {0}", data);
                                data = this.Update(connection, data, principal);
                            }
                            else
                                throw new DuplicateNameException(data.Key?.ToString());
                        }
                        else
                        {
                            this.m_tracer.TraceEvent(TraceEventType.Verbose, 0, "INSERT {0}", data);
                            data = this.Insert(connection, data, principal);
                        }

                        if (mode == TransactionMode.Commit)
                        {
                            tx.Commit();
                            foreach (var itm in connection.CacheOnCommit)
                                ApplicationContext.Current.GetService<IDataCachingService>()?.Add(itm);
                        }
                        else
                            tx.Rollback();

                        var args = new PostPersistenceEventArgs<TData>(data, principal)
                        {
                            Mode = mode
                        };

                        this.Inserted?.Invoke(this, args);

                        return data;

                    }
                    catch (Exception e)
                    {
                        this.m_tracer.TraceEvent(TraceEventType.Error, 0, "Error : {0}", e);
                        tx?.Rollback();
                        throw new DataPersistenceException(e.Message, e);
                    }
                    finally
                    {
                    }
            }
        }

        /// <summary>
        /// Update the specified object
        /// </summary>
        /// <param name="storageData"></param>
        /// <param name="principal"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public TData Update(TData data, IPrincipal principal, TransactionMode mode)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            else if (data.Key == Guid.Empty)
                throw new InvalidOperationException("Data missing key");

            PrePersistenceEventArgs<TData> preArgs = new PrePersistenceEventArgs<TData>(data, principal);
            this.Updating?.Invoke(this, preArgs);
            if (preArgs.Cancel)
            {
                this.m_tracer.TraceEvent(TraceEventType.Warning, 0, "Pre-Event handler indicates abort update for {0}", data);
                return data;
            }

            // Persist object
            using (var connection = m_configuration.Provider.GetWriteConnection())
            {
                connection.Open();
                using (IDbTransaction tx = connection.BeginTransaction())
                    try
                    {
                        //connection.Connection.Open();

                        this.m_tracer.TraceEvent(TraceEventType.Verbose, 0, "UPDATE {0}", data);

                        data = this.Update(connection, data, principal);

                        if (mode == TransactionMode.Commit)
                        {
                            tx.Commit();
                            foreach (var itm in connection.CacheOnCommit)
                                ApplicationContext.Current.GetService<IDataCachingService>()?.Add(itm);

                        }
                        else
                            tx.Rollback();

                        var args = new PostPersistenceEventArgs<TData>(data, principal)
                        {
                            Mode = mode
                        };

                        this.Updated?.Invoke(this, args);

                        return data;
                    }
                    catch (Exception e)
                    {
                        this.m_tracer.TraceEvent(TraceEventType.Error, 0, "Error : {0}", e);
                        tx?.Rollback();
                        throw new DataPersistenceException(e.Message, e);

                    }
                    finally
                    {
                    }

            }
        }

        /// <summary>
        /// Obsoletes the specified object
        /// </summary>
        public TData Obsolete(TData data, IPrincipal principal, TransactionMode mode)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            else if (data.Key == Guid.Empty)
                throw new InvalidOperationException("Data missing key");

            PrePersistenceEventArgs<TData> preArgs = new PrePersistenceEventArgs<TData>(data);
            this.Obsoleting?.Invoke(this, preArgs);
            if (preArgs.Cancel)
            {
                this.m_tracer.TraceEvent(TraceEventType.Warning, 0, "Pre-Event handler indicates abort for {0}", data);
                return data;
            }

            // Obsolete object
            using (var connection = m_configuration.Provider.GetWriteConnection())
            {
                connection.Open();
                using (IDbTransaction tx = connection.BeginTransaction())
                    try
                    {
                        //connection.Connection.Open();

                        this.m_tracer.TraceEvent(TraceEventType.Verbose, 0, "OBSOLETE {0}", data);

                        data = this.Obsolete(connection, data, principal);

                        if (mode == TransactionMode.Commit)
                        {
                            tx.Commit();
                            foreach (var itm in connection.CacheOnCommit)
                                ApplicationContext.Current.GetService<IDataCachingService>()?.Remove(itm.GetType(), itm.Key.Value);
                        }
                        else
                            tx.Rollback();

                        var args = new PostPersistenceEventArgs<TData>(data, principal)
                        {
                            Mode = mode
                        };

                        this.Obsoleted?.Invoke(this, args);

                        return data;
                    }
                    catch (Exception e)
                    {
                        this.m_tracer.TraceEvent(TraceEventType.Error, 0, "Error : {0}", e);
                        tx?.Rollback();
                        throw new DataPersistenceException(e.Message, e);
                    }
                    finally
                    {
                    }

            }
        }

        /// <summary>
        /// Gets the specified object
        /// </summary>
        public virtual TData Get<TIdentifier>(MARC.HI.EHRS.SVC.Core.Data.Identifier<TIdentifier> containerId, IPrincipal principal, bool loadFast)
        {
            // Try the cache if available
            var guidIdentifier = containerId as Identifier<Guid>;
            var cacheItem = ApplicationContext.Current.GetService<IDataCachingService>()?.GetCacheItem<TData>(guidIdentifier.Id) as TData;
            if (loadFast && cacheItem != null)
            {
                return cacheItem;
            }
            else
            {

#if DEBUG
                Stopwatch sw = new Stopwatch();
                sw.Start();
#endif

                PreRetrievalEventArgs preArgs = new PreRetrievalEventArgs(containerId, principal);
                this.Retrieving?.Invoke(this, preArgs);
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

                        var result = this.Get(connection, guidIdentifier.Id, principal);
                        var postData = new PostRetrievalEventArgs<TData>(result, principal);
                        this.Retrieved?.Invoke(this, postData);

                        foreach (var itm in connection.CacheOnCommit)
                            ApplicationContext.Current.GetService<IDataCachingService>()?.Add(itm);

                        return result;

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
        }

        /// <summary>
        /// Performs the specified query
        /// </summary>
        public int Count(Expression<Func<TData, bool>> query, IPrincipal authContext)
        {
            var tr = 0;
            this.Query(query, 0, null, authContext, out tr);
            return tr;
        }

        /// <summary>
        /// Performs query returning all results
        /// </summary>
        public virtual IEnumerable<TData> Query(Expression<Func<TData, bool>> query, IPrincipal authContext)
        {
            var tr = 0;
            return this.QueryInternal(query, 0, null, authContext, out tr, true);

        }

        /// <summary>
        /// Performs the specified query
        /// </summary>
        public virtual IEnumerable<TData> Query(Expression<Func<TData, bool>> query, int offset, int? count, IPrincipal authContext, out int totalCount)
        {
            return this.QueryInternal(query, offset, count, authContext, out totalCount, false);
        }

        /// <summary>
        /// Instructs the service 
        /// </summary>
        protected virtual IEnumerable<TData> QueryInternal(Expression<Func<TData, bool>> query, int offset, int? count, IPrincipal authContext, out int totalCount, bool fastQuery)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

#if DEBUG
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif

            PreQueryEventArgs<TData> preArgs = new PreQueryEventArgs<TData>(query, authContext);
            this.Querying?.Invoke(this, preArgs);
            if (preArgs.Cancel)
            {
                this.m_tracer.TraceEvent(TraceEventType.Warning, 0, "Pre-Event handler indicates abort query {0}", query);
                totalCount = 0;
                return null;
            }

            // Query object
            using (var connection = m_configuration.Provider.GetReadonlyConnection())
                try
                {
                    connection.Open();

                    this.m_tracer.TraceEvent(TraceEventType.Verbose, 0, "QUERY {0}", query);

                    var results = this.Query(connection, query, offset, count ?? 1000, out totalCount, true, authContext);
                    var postData = new PostQueryEventArgs<TData>(query, results.AsQueryable(), authContext);
                    this.Queried?.Invoke(this, postData);

                    var retVal = postData.Results.AsParallel().ToList();
                    foreach (var itm in connection.CacheOnCommit)
                        ApplicationContext.Current.GetService<IDataCachingService>()?.Add(itm);

                    this.m_tracer.TraceEvent(TraceEventType.Verbose, 0, "Returning {0}..{1} or {2} results", offset, offset + (count ?? 1000), totalCount);

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
                    this.m_tracer.TraceEvent(TraceEventType.Verbose, 0, "Query {0} took {1} ms", query, sw.ElapsedMilliseconds);
#endif
                }
        }


        /// <summary>
        /// Performthe actual insert.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public TData Insert(DataContext context, TData data, IPrincipal principal)
        {
            var retVal = this.InsertInternal(context, data, principal);
            //if (retVal != data) System.Diagnostics.Debugger.Break();
            context.AddCacheCommit(retVal);
            return retVal;
        }
        /// <summary>
        /// Perform the actual update.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public TData Update(DataContext context, TData data, IPrincipal principal)
        {
            //// Make sure we're updating the right thing
            //if (data.Key.HasValue)
            //{
            //    var cacheItem = ApplicationContext.Current.GetService<IDataCachingService>()?.GetCacheItem(data.GetType(), data.Key.Value);
            //    if (cacheItem != null)
            //    {
            //        cacheItem.CopyObjectData(data);
            //        data = cacheItem as TData;
            //    }
            //}

            var retVal = this.UpdateInternal(context, data, principal);
            //if (retVal != data) System.Diagnostics.Debugger.Break();
            context.AddCacheCommit(retVal);
            return retVal;

        }
        /// <summary>
        /// Performs the actual obsoletion
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public TData Obsolete(DataContext context, TData data, IPrincipal principal)
        {
            var retVal = this.ObsoleteInternal(context, data, principal);
            //if (retVal != data) System.Diagnostics.Debugger.Break();
            context.AddCacheCommit(retVal);
            return retVal;
        }
        /// <summary>
        /// Performs the actual query
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="query">Query.</param>
        public IEnumerable<TData> Query(DataContext context, Expression<Func<TData, bool>> query, int offset, int count, out int totalResults, bool countResults, IPrincipal principal)
        {
            var retVal = this.QueryInternal(context, query, offset, count, out totalResults, principal, countResults);
            foreach (var i in retVal.Where(i => i != null))
            {
                context.AddCacheCommit(i);
            }
            return retVal;

        }
       

        /// <summary>
        /// Insert the object for generic methods
        /// </summary>
        object IAdoPersistenceService.Insert(DataContext context, object data, IPrincipal principal)
        {
            return this.InsertInternal(context, (TData)data, principal);
        }

        /// <summary>
        /// Update the object for generic methods
        /// </summary>
        object IAdoPersistenceService.Update(DataContext context, object data, IPrincipal principal)
        {
            return this.UpdateInternal(context, (TData)data, principal);
        }

        /// <summary>
        /// Obsolete the object for generic methods
        /// </summary>
        object IAdoPersistenceService.Obsolete(DataContext context, object data, IPrincipal principal)
        {
            return this.ObsoleteInternal(context, (TData)data, principal);
        }

        /// <summary>
        /// Get the specified data
        /// </summary>
        object IAdoPersistenceService.Get(DataContext context, Guid id, IPrincipal principal)
        {
            return this.Get(context, id, principal);
        }

        /// <summary>
        /// Insert the object
        /// </summary>
        object IDataPersistenceService.Insert(object data)
        {
            return this.Insert((TData)data, AuthenticationContext.Current.Principal, TransactionMode.Commit);
        }

        /// <summary>
        /// Update the specified data
        /// </summary>
        object IDataPersistenceService.Update(object data)
        {
            return this.Update((TData)data, AuthenticationContext.Current.Principal, TransactionMode.Commit);
        }

        /// <summary>
        /// Obsolete specified data
        /// </summary>
        object IDataPersistenceService.Obsolete(object data)
        {
            return this.Obsolete((TData)data, AuthenticationContext.Current.Principal, TransactionMode.Commit);
        }

        /// <summary>
        /// Get the specified data
        /// </summary>
        object IDataPersistenceService.Get(Guid id)
        {
            return this.Get(new Identifier<Guid>(id, Guid.Empty), AuthenticationContext.Current.Principal, false);
        }

        /// <summary>
        /// Generic to model instance for other callers
        /// </summary>
        /// <returns></returns>
        object IAdoPersistenceService.ToModelInstance(object domainInstance, DataContext context, IPrincipal principal)
        {
            return this.ToModelInstance(domainInstance, context, principal);
        }

        /// <summary>
        /// Perform generic query
        /// </summary>
        IEnumerable IDataPersistenceService.Query(Expression query, int offset, int? count, out int totalResults)
        {
            return this.Query((Expression<Func<TData, bool>>)query, offset, count, AuthenticationContext.Current.Principal, out totalResults);
        }


        #region Event Handler Helpers

        /// <summary>
        /// Fire retrieving
        /// </summary>
        protected void FireRetrieving(PreRetrievalEventArgs e)
        {
            this.Retrieving?.Invoke(this, e);
        }

        /// <summary>
        /// Fire retrieving
        /// </summary>
        protected void FireRetrieved(PostRetrievalEventArgs<TData> e)
        {
            this.Retrieved?.Invoke(this, e);
        }

        #endregion

    }
}

