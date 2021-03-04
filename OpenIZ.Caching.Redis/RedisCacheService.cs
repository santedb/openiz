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
using Newtonsoft.Json;
using OpenIZ.Caching.Redis.Configuration;
using OpenIZ.Core.Diagnostics;
using OpenIZ.Core.Model;
using OpenIZ.Core.Model.Acts;
using OpenIZ.Core.Model.Attributes;
using OpenIZ.Core.Model.Constants;
using OpenIZ.Core.Model.DataTypes;
using OpenIZ.Core.Model.Entities;
using OpenIZ.Core.Model.Interfaces;
using OpenIZ.Core.Model.Serialization;
using OpenIZ.Core.Services;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace OpenIZ.Caching.Redis
{
    /// <summary>
    /// Redis memory caching service
    /// </summary>
    [Description("REDIS Data Caching Service")]
    public class RedisCacheService : IDataCachingService, IDaemonService
    {

        // Redis trace source
        private TraceSource m_tracer = new TraceSource("OpenIZ.Caching.Redis");

        // Serializer
        private Dictionary<Type, XmlSerializer> m_serializerCache = new Dictionary<Type, XmlSerializer>();

        // Connection
        private ConnectionMultiplexer m_connection;

        // Subscriber
        private ISubscriber m_subscriber;

        // Configuration
        private RedisConfiguration m_configuration = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection("openiz.caching.redis") as RedisConfiguration;

        // Binder
        private ModelSerializationBinder m_binder = new ModelSerializationBinder();

        // Non cached types
        private HashSet<Type> m_nonCached = new HashSet<Type>();

        /// <summary>
        /// Is the service running 
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return this.m_connection != null;
            }
        }

        // Data was added to the cache
        public event EventHandler<DataCacheEventArgs> Added;
        // Data was removed from the cache
        public event EventHandler<DataCacheEventArgs> Removed;
        // Started 
        public event EventHandler Started;
        // Starting
        public event EventHandler Starting;
        // Stopped
        public event EventHandler Stopped;
        // Stopping
        public event EventHandler Stopping;
        // Data was updated on the cache
        public event EventHandler<DataCacheEventArgs> Updated;


        /// <summary>
        /// Serialize objects
        /// </summary>
        private HashEntry[] SerializeObject(IdentifiedData data)
        {
            XmlSerializer xsz = null;
            if (!this.m_serializerCache.TryGetValue(data.GetType(), out xsz))
            {
                xsz = new XmlSerializer(data.GetType());
                lock (this.m_serializerCache)
                    if (!this.m_serializerCache.ContainsKey(data.GetType()))
                        this.m_serializerCache.Add(data.GetType(), xsz);
            }

            HashEntry[] retVal = new HashEntry[3];
            retVal[0] = new HashEntry("type", data.GetType().AssemblyQualifiedName);
            retVal[1] = new HashEntry("loadState", (int)data.LoadState);
            using (var sw = new StringWriter())
            {
                xsz.Serialize(sw, data);
                retVal[2] = new HashEntry("value", sw.ToString());
            }
            return retVal;
        }

        /// <summary>
        /// Serialize objects
        /// </summary>
        private IdentifiedData DeserializeObject(HashEntry[] data)
        {

            if (data == null || data.Length == 0) return null;

            Type type = Type.GetType(data.FirstOrDefault(o => o.Name == "type").Value);
            LoadState ls = (LoadState)(int)data.FirstOrDefault(o => o.Name == "loadState").Value;
            String value = data.FirstOrDefault(o => o.Name == "value").Value;

            // Find serializer
            XmlSerializer xsz = null;
            if (!this.m_serializerCache.TryGetValue(type, out xsz))
            {
                xsz = new XmlSerializer(type);
                lock (this.m_serializerCache)
                    if (!this.m_serializerCache.ContainsKey(type))
                        this.m_serializerCache.Add(type, xsz);
            }
            using (var sr = new StringReader(value))
            {
                var retVal = xsz.Deserialize(sr) as IdentifiedData;
                retVal.LoadState = ls;
                return retVal;
            }

        }

        /// <summary>
        /// Ensure cache consistency
        /// </summary>
        private void EnsureCacheConsistency(DataCacheEventArgs e, bool remove = false)
        {
            // If someone inserts a relationship directly, we need to unload both the source and target so they are re-loaded 
            if (e.Object is ActParticipation ptcpt)
            {
                this.Remove(ptcpt.SourceEntityKey.GetValueOrDefault());
                this.Remove(ptcpt.PlayerEntityKey.GetValueOrDefault());
                //MemoryCache.Current.RemoveObject(ptcpt.PlayerEntity?.GetType() ?? typeof(Entity), ptcpt.PlayerEntityKey);
            }
            else if (e.Object is ActRelationship actrel)
            {
                this.Remove(actrel.SourceEntityKey.GetValueOrDefault());
                this.Remove(actrel.TargetActKey.GetValueOrDefault());
            }
            else if (e.Object is EntityRelationship entrel)
            {
                this.Remove(entrel.SourceEntityKey.GetValueOrDefault());
                this.Remove(entrel.TargetEntityKey.GetValueOrDefault());
            }

        }

        /// <summary>
        /// Add an object to the REDIS cache
        /// </summary>
        public void Add(IdentifiedData data)
        {
            try
            {

                // We want to add only those when the connection is present
                if (this.m_connection == null || data == null || !data.Key.HasValue ||
                    (data as BaseEntityData)?.ObsoletionTime.HasValue == true ||
                    this.m_nonCached.Contains(data.GetType()))
                {
                    this.m_tracer.TraceVerbose("Skipping caching of {0} (OBS:{1}, NCC:{2})",
                        data, (data as BaseEntityData)?.ObsoletionTime.HasValue == true, this.m_nonCached.Contains(data.GetType()));
                    return;
                }

                // Only add data which is an entity, act, or relationship
                //if (data is Act || data is Entity || data is ActRelationship || data is ActParticipation || data is EntityRelationship || data is Concept)
                //{
                // Add

                var redisDb = this.m_connection.GetDatabase();
                //var existing = redisDb.KeyExists(data.Key.Value.ToString());
                redisDb.HashSet(data.Key.Value.ToString(), this.SerializeObject(data), CommandFlags.FireAndForget);
#if DEBUG
                this.m_tracer.TraceVerbose("HashSet {0} (EXIST: {1}; @: {2})", data, false, new System.Diagnostics.StackTrace(true).GetFrame(1));
#endif 
                redisDb.KeyExpire(data.Key.Value.ToString(), new TimeSpan(1, 0, 0), CommandFlags.FireAndForget);
                this.EnsureCacheConsistency(new DataCacheEventArgs(data));
                //if (existing)
                //    this.m_connection.GetSubscriber().Publish("oiz.events", $"PUT http://{Environment.MachineName}/cache/{data.Key.Value}");
                //else
                this.m_connection.GetSubscriber().Publish("oiz.events", $"POST http://{Environment.MachineName}/cache/{data.Key.Value}");
                //}
            }
            catch (Exception e)
            {
                this.m_tracer.TraceWarning("REDIS CACHE ERROR (CACHING SKIPPED): {0}", e);
            }
        }

        /// <summary>
        /// Get a cache item
        /// </summary>
        public object GetCacheItem(Guid key)
        {
            try
            {
                // We want to add
                if (this.m_connection == null)
                    return null;

                // Add
                var redisDb = this.m_connection.GetDatabase();
                redisDb.KeyExpire(key.ToString(), new TimeSpan(1, 0, 0), CommandFlags.FireAndForget);
                return this.DeserializeObject(redisDb.HashGetAll(key.ToString()));
            }
            catch (Exception e)
            {
                this.m_tracer.TraceWarning("REDIS CACHE ERROR (FETCHING SKIPPED): {0}", e);
                return null;
            }

        }

        /// <summary>
        /// Get cache item of type
        /// </summary>
        public TData GetCacheItem<TData>(Guid key) where TData : IdentifiedData
        {
            var retVal = this.GetCacheItem(key);
            if (retVal is TData)
                return (TData)retVal;
            else return null;
        }

        /// <summary>
        /// Remove a hash key item
        /// </summary>
        public void Remove(Guid key)
        {
            // We want to add
            if (this.m_connection == null)
                return;
            // Add
            var existing = this.GetCacheItem(key);
            var redisDb = this.m_connection.GetDatabase();
            redisDb.KeyDelete(key.ToString(), CommandFlags.FireAndForget);
            this.EnsureCacheConsistency(new DataCacheEventArgs(existing), true);

            this.m_connection.GetSubscriber().Publish("oiz.events", $"DELETE http://{Environment.MachineName}/cache/{key}");
        }

        /// <summary>
        /// Start the connection manager
        /// </summary>
        public bool Start()
        {
            try
            {
                this.Starting?.Invoke(this, EventArgs.Empty);

                this.m_tracer.TraceInfo("Starting REDIS cache service to hosts {0}...", String.Join(";", this.m_configuration.Servers));

                var configuration = new ConfigurationOptions()
                {
                    AbortOnConnectFail = false,
                    Password = this.m_configuration.Password
                };
                foreach (var itm in this.m_configuration.Servers)
                    configuration.EndPoints.Add(itm);

                this.m_connection = ConnectionMultiplexer.Connect(configuration);
                this.m_subscriber = this.m_connection.GetSubscriber();
                // Look for non-cached types
                foreach (var itm in typeof(IdentifiedData).Assembly.GetTypes().Where(o => o.GetCustomAttribute<NonCachedAttribute>() != null ||
                    (o.GetCustomAttribute<XmlRootAttribute>() == null && !typeof(IVersionedAssociation).IsAssignableFrom(o))))
                    this.m_nonCached.Add(itm);

                // Subscribe to OpenIZ events
                m_subscriber.Subscribe("oiz.events", (channel, message) =>
                {

                    this.m_tracer.TraceVerbose("Received event {0} on {1}", message, channel);

                    var messageParts = ((string)message).Split(' ');
                    var verb = messageParts[0];
                    var uri = new Uri(messageParts[1]);

                    string resource = uri.AbsolutePath.Replace("imsi/", ""),
                        id = uri.AbsolutePath.Substring(uri.AbsolutePath.LastIndexOf("/") + 1);

                    switch (verb.ToLower())
                    {
                        case "post":
                            this.Added?.Invoke(this, new DataCacheEventArgs(this.GetCacheItem(Guid.Parse(id))));
                            break;
                        case "put":
                            this.Updated?.Invoke(this, new DataCacheEventArgs(this.GetCacheItem(Guid.Parse(id))));
                            break;
                        case "delete":
                            this.Removed?.Invoke(this, new DataCacheEventArgs(id));
                            break;
                    }
                });

                this.Started?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error starting REDIS caching, will switch to no-caching : {0}", e);
                ApplicationContext.Current.RemoveServiceProvider(typeof(RedisCacheService));
                ApplicationContext.Current.RemoveServiceProvider(typeof(IDataCachingService));
                return false;
            }
        }

        /// <summary>
        /// Stop the connection
        /// </summary>
        public bool Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);
            this.m_connection.Dispose();
            this.m_connection = null;
            this.Stopped?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Clear the cache
        /// </summary>
        public void Clear()
        {
            try
            {
                this.m_connection.GetServer(this.m_configuration.Servers.First()).FlushAllDatabases();
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Could not flush REDIS database: {0}", e);
            }
        }

        /// <summary>
        /// Size of the database
        /// </summary>
        public long Size
        {
            get
            {
                return this.m_connection.GetServer(this.m_configuration.Servers.First()).DatabaseSize();
            }
        }
    }
}
