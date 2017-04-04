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
using MARC.Everest.Threading;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Event;
using MARC.HI.EHRS.SVC.Core.Services;
using OpenIZ.Caching.Memory.Configuration;
using OpenIZ.Core.Model;
using OpenIZ.Core.Model.Collection;
using OpenIZ.Core.Model.Interfaces;
using OpenIZ.Core.Model.Query;
using OpenIZ.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenIZ.Caching.Memory
{
    /// <summary>
    /// Memory cache functions
    /// </summary>
    public class MemoryCache : IDisposable
    {

        // Entry table for the cache
        private Dictionary<Type, Dictionary<Guid, CacheEntry>> m_entryTable = new Dictionary<Type, Dictionary<Guid, CacheEntry>>();
        
        // True if the object is disposed
        private bool m_disposed = false;

        // The lockbox used by the cache to ensure entries aren't written at the same time
        private object m_lock = new object();

        // Configuration of the ccache
        private MemoryCacheConfiguration m_configuration = ApplicationContext.Current?.GetService<IConfigurationManager>()?.GetSection("openiz.caching.memory") as MemoryCacheConfiguration ?? new MemoryCacheConfiguration();

        // Tracer for logging
        private TraceSource m_tracer = new TraceSource("OpenIZ.Caching.Memory");

        // Current singleton
        private static MemoryCache s_current = null;

        // Lockbox for singleton creation
        private static object s_lock = new object();

        // Thread pool for cleanup tasks
        private WaitThreadPool m_taskPool = new WaitThreadPool(2);

        // Minimum age of a cache item (helps when large queries are returned)
        private long m_minAgeTicks = new TimeSpan(0, 0, 30).Ticks;

        // Lock object used by cleaning mechanisms to run one at a time
        private object m_cacheCleanLock = new object();

        /// <summary>
        /// Memory cache configuration
        /// </summary>
        private MemoryCache()
        {
            m_tracer.TraceInformation("Binding initial collections...");

            foreach (var t in this.m_configuration.Types)
            {
                this.RegisterCacheType(t.Type);
                // TODO: Initialize the cache
                ApplicationContext.Current.Started += (o, e) =>
                {
                    ApplicationContext.Current.GetService<IThreadPoolService>().QueueUserWorkItem(x =>
                    {
                        var xt = (x as TypeCacheConfigurationInfo);
                        this.m_tracer.TraceEvent(TraceEventType.Information, 0, "Initialize cache for {0}", xt.Type);
                        var idpInstance = ApplicationContext.Current.GetService(typeof(IDataPersistenceService<>).MakeGenericType(xt.Type)) as IDataPersistenceService;
                        if(idpInstance != null)
                            foreach(var itm in xt.SeedQueries)
                            {
                                this.m_tracer.TraceEvent(TraceEventType.Verbose, 0, itm);
                                var query = typeof(QueryExpressionParser).GetGenericMethod("BuildLinqExpression", new Type[] { xt.Type }, new Type[] { typeof(NameValueCollection) }).Invoke(null, new object[] { NameValueCollection.ParseQueryString(itm) }) as Expression;
                                int offset = 0, totalResults = 1;
                                while (offset < totalResults)
                                {
                                    idpInstance.Query(query, offset, 100, out totalResults);
                                    offset += 100;
                                }
                            }
                    }, t);
                };
            }
        }

        /// <summary>
        /// Gets the current singleton
        /// </summary>
        public static MemoryCache Current
        {
            get
            {
                if (s_current == null)
                    lock (s_lock)
                        if (s_current == null)
                            s_current = new MemoryCache();
                return s_current;
            }
        }

        /// <summary>
        /// Gets the size of the current cache
        /// </summary>
        public int GetSize(Type t)
        {
            this.ThrowIfDisposed();

            Dictionary<Guid, CacheEntry> cache = null;
            if (this.m_entryTable.TryGetValue(t, out cache))
                return cache.Count;
            else
                return 0;

        }

        /// <summary>
        /// Adds an entry or updates it
        /// </summary>
        public void AddUpdateEntry(object data)
        {

            // Throw if disposed
            this.ThrowIfDisposed();

            var idData = data as IdentifiedData;
            if (idData == null || idData.IsEmpty() == true || !idData.Key.HasValue)
                return;
            var vidData = data as IBaseEntityData;

            Type objData = data?.GetType();
            if (idData == null || !idData.Key.HasValue ||
                vidData?.ObsoletionTime.HasValue == true || vidData?.CreationTime == default(DateTimeOffset))
                return;

            Dictionary<Guid, CacheEntry> cache = null;
            if (this.m_entryTable.TryGetValue(objData, out cache))
            {
                // We want to cascade up the type heirarchy this is a do/while with IF instead of while
                // because the ELSE-IF clause
                do
                {
                    Guid key = idData?.Key ?? Guid.Empty;
                    if (cache.ContainsKey(key))
                        lock (this.m_lock)
                        {
                            cache[key].Update(data);
                        }
                    else
                        lock (this.m_lock)
                            if (!cache.ContainsKey(key))
                            {
                                cache.Add(key, new CacheEntry(DateTime.Now, data));
                                this.m_tracer.TraceEvent(TraceEventType.Verbose, 0, "Cache for {0} contains {1} entries...", objData, cache.Count);
                            }

                    objData = objData.BaseType;
                } while (this.m_entryTable.TryGetValue(objData, out cache));

            }
            else if (this.m_configuration.AutoSubscribeTypes)
                this.RegisterCacheType(data.GetType());

        }

        /// <summary>
        /// Try to get an entry from the cache returning null if not found
        /// </summary>
        public object TryGetEntry(Type objectType, Guid? key)
        {
            this.ThrowIfDisposed();

            if (!key.HasValue) return null;
            else if (objectType == null)
                throw new ArgumentNullException(nameof(objectType));

            Dictionary<Guid, CacheEntry> cache = null;
            if(this.m_entryTable.TryGetValue(objectType, out cache))
            {
                CacheEntry candidate = default(CacheEntry);
                if (cache.TryGetValue(key.Value, out candidate))
                {
                    candidate.Touch();
                    this.m_tracer.TraceEvent(TraceEventType.Verbose, 0, "Cache hit {0} = {1}", key.Value, candidate.Data);
                    return candidate.Data;
                }
            }
            this.m_tracer.TraceEvent(TraceEventType.Verbose, 0, "Cache miss {0}", key.Value);
            return null;
        }

        /// <summary>
        /// Sets the minimum age
        /// </summary>
        /// <param name="age"></param>
        public  void SetMinAge(TimeSpan age)
        {
            this.ThrowIfDisposed();

            this.m_minAgeTicks = age.Ticks;
        }

        /// <summary>
        /// If a cache is reaching its maximum entry level clean some space 
        /// </summary>
        public void ReducePressure()
        {
            this.ThrowIfDisposed();

            if (!Monitor.TryEnter(this.m_cacheCleanLock))
                return; // Something else is locking the process

            // Entry table clean
            lock (this.m_cacheCleanLock)
            {
                this.m_tracer.TraceEvent(TraceEventType.Information, 0, "Starting memory cache pressure reduction...");
                var nowTicks = DateTime.Now.Ticks;

                foreach (var itm in this.m_entryTable)
                {
                    var config = this.m_configuration.Types.FirstOrDefault(o => o.Type == itm.Key);
                    int maxSize = (config?.MaxCacheSize ?? 50);
                    var garbageBin = itm.Value.AsParallel().OrderByDescending(o => o.Value.LastUpdateTime).Take(itm.Value.Count - maxSize).Where(o => (nowTicks - o.Value.LastUpdateTime) >= this.m_minAgeTicks).Select(o => o.Key);

                    if (garbageBin.Count() > 0)
                    {
                        this.m_tracer.TraceEvent(TraceEventType.Verbose, 0, "Cache {0} overcommitted by {1} will remove entries older than min age..", garbageBin.Count(), itm.Key.FullName);

                        this.m_taskPool.QueueUserWorkItem((o) =>
                        {
                            IEnumerable<Guid> gc = o as IEnumerable<Guid>;
                            foreach (var g in gc.ToArray())
                                lock (this.m_lock)
                                    itm.Value.Remove(g);
                        }, garbageBin);
                    }
                }
            }
        }

        /// <summary>
        /// Remove the specified object from the cache
        /// </summary>
        public void RemoveObject(Type objectType, Guid? key)
        {
            this.ThrowIfDisposed();

            if (!key.HasValue) return;
            else if (objectType == null)
                throw new ArgumentNullException(nameof(objectType));

            Dictionary<Guid, CacheEntry> cache = null;
            if (this.m_entryTable.TryGetValue(objectType, out cache))
            {
                CacheEntry candidate = default(CacheEntry);
                if (cache.TryGetValue(key.Value, out candidate))
                {
                    lock (this.m_lock)
                    {
                        cache.Remove(key.Value);
                    }
                }
            }
            return;
        }

        /// <summary>
        /// Clears all caches
        /// </summary>
        public void Clear()
        {
            this.ThrowIfDisposed();

            foreach (var itm in this.m_entryTable)
                lock (this.m_lock)
                    itm.Value.Clear();
        }


        /// <summary>
        /// Clean the cache of old entries
        /// </summary>
        public void Clean()
        {
            this.ThrowIfDisposed();

            long nowTicks = DateTime.Now.Ticks;
            lock (this.m_cacheCleanLock) // This time wait for a lock
            {
                this.m_tracer.TraceEvent(TraceEventType.Information, 0, "Starting memory cache deep clean...");

                // Entry table clean
                foreach (var itm in this.m_entryTable)
                {
                    var config = this.m_configuration.Types.FirstOrDefault(o => o.Type == itm.Key);
                    if (config == null) continue;

                    // Clean old data
                    var garbageBin = itm.Value.AsParallel().Where(o => nowTicks - o.Value.LastUpdateTime > config.MaxCacheAge).Select(o => o.Key);
                    if (garbageBin.Count() > 0)
                    {
                        this.m_tracer.TraceEvent(TraceEventType.Verbose, 0, "Will clean {0} stale entries from cache {1}..", garbageBin.Count(), itm.Key.FullName);
                        this.m_taskPool.QueueUserWorkItem((o) =>
                        {
                            IEnumerable<Guid> gc = o as IEnumerable<Guid>;
                            foreach (var g in gc)
                                lock (this.m_lock)
                                    itm.Value.Remove(g);
                        }, garbageBin);
                    }

                }
            }
        }

        /// <summary>
        /// Register caching type
        /// </summary>
        public void RegisterCacheType(Type t, int maxSize = 50, long maxAge = 0x23C34600)
        {

            this.ThrowIfDisposed();

            // Lock the master cache 
            Dictionary<Guid, CacheEntry> cache = null;
            lock (this.m_lock)
            {
                if (!this.m_entryTable.TryGetValue(t, out cache))
                {
                    cache = new Dictionary<Guid, CacheEntry>(10);
                    if(!this.m_configuration.Types.Exists(o=>o.Type == t))
                        this.m_configuration.Types.Add(new TypeCacheConfigurationInfo() { Type = t, MaxCacheSize = maxSize, MaxCacheAge = maxAge });
                    this.m_entryTable.Add(t, cache);
                }
                else
                    return;
            }

            // We want to subscribe when this object is changed so we can keep the cache fresh
            var idpType = typeof(IDataPersistenceService<>).MakeGenericType(t);
            var ppeArgType = typeof(PostPersistenceEventArgs<>).MakeGenericType(t);
            var evtHdlrType = typeof(EventHandler<>).MakeGenericType(ppeArgType);
            var svcInstance = ApplicationContext.Current.GetService(idpType);

            if (svcInstance != null)
            {
                // Construct the delegate
                var senderParm = Expression.Parameter(typeof(Object), "o");
                var eventParm = Expression.Parameter(ppeArgType, "e");
                var eventTxMode = Expression.MakeMemberAccess(eventParm, ppeArgType.GetProperty("Mode"));
                var eventData = Expression.MakeMemberAccess(eventParm, ppeArgType.GetProperty("Data"));
                var handlerLambda = Expression.Lambda(evtHdlrType, Expression.Call(Expression.Constant(this), typeof(MemoryCache).GetMethod("HandlePostPersistenceEvent"),
                    eventTxMode, eventData), senderParm, eventParm);
                var delInstance = handlerLambda.Compile();

                // Bind to events
                idpType.GetEvent("Inserted").AddEventHandler(svcInstance, delInstance);
                idpType.GetEvent("Updated").AddEventHandler(svcInstance, delInstance);
                idpType.GetEvent("Obsoleted").AddEventHandler(svcInstance, delInstance);
            }


            // Load initial data
            this.m_taskPool.QueueUserWorkItem((o) =>
            {
                lock (s_lock)
                {
                    var conf = this.m_configuration.Types.ToArray().FirstOrDefault(c => c.Type == t);
                    if (conf == null) return;
                    foreach (var sd in conf.SeedQueries)
                    {
                        this.m_tracer.TraceInformation("Seeding cache with {0}", sd);
                        // TODO: Seed cache initial data
                    }
                }
            });
        }

        /// <summary>
        /// Object is disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (this.m_disposed)
                throw new ObjectDisposedException(nameof(MemoryCache));
        }

        /// <summary>
        /// Persistence event handler
        /// </summary>
        public void HandlePostPersistenceEvent(TransactionMode txMode, Object data)
        {
            if (txMode == TransactionMode.Commit)
            {
                // Bundles are special cases.
                if (data is Bundle)
                {
                    foreach (var itm in (data as Bundle).Item)
                        HandlePostPersistenceEvent(txMode, itm);
                }
                else
                {
                    //this.RemoveObject(data.GetType(), (data as IIdentifiedEntity).Key.Value);
                    var idData = data as IIdentifiedEntity;
                    var objData = data.GetType();

                    Dictionary<Guid, CacheEntry> cache = null;
                    if (this.m_entryTable.TryGetValue(objData, out cache))
                    {
                        Guid key = idData?.Key ?? Guid.Empty;
                        if (cache.ContainsKey(key))
                            lock (this.m_lock)
                            {
                                cache[key].Update(data);
                            }
                        //cache.Remove(key);
                    }
                }
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {

            this.m_taskPool.Dispose();
            this.m_disposed = true;
        }
    }
}
