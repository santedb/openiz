﻿using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Data;
using MARC.HI.EHRS.SVC.Core.Services;
using OpenIZ.Caching.Redis.Configuration;
using OpenIZ.Core.Diagnostics;
using OpenIZ.Core.Interfaces;
using OpenIZ.Core.Services;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Caching.Redis
{
    /// <summary>
    /// Represents a REDIS based query persistence service
    /// </summary>
    public class RedisQueryPersistenceService : MARC.HI.EHRS.SVC.Core.Services.IQueryPersistenceService, IDaemonService
    {

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "REDIS Query Persistence Service";

        /// <summary>
        /// True if service is running
        /// </summary>
        public bool IsRunning => this.m_configuration != null;

        // Redis trace source
        private TraceSource m_tracer = new TraceSource(RedisCacheConstants.TraceSourceName);

        // Connection
        private ConnectionMultiplexer m_connection;

        /// <summary>
        /// Query tag in a hash set
        /// </summary>
        private const int FIELD_QUERY_TAG_IDX = 0;
        /// <summary>
        /// Query total results 
        /// </summary>
        private const int FIELD_QUERY_TOTAL_RESULTS = 1;
        /// <summary>
        /// Query result index
        /// </summary>
        private const int FIELD_QUERY_RESULT_IDX = 2;


        // Configuration
        private RedisConfiguration m_configuration = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection("openiz.caching.redis") as RedisConfiguration;
        
        /// <summary>
        /// Application daemon is starting
        /// </summary>
        public event EventHandler Starting;
        /// <summary>
        /// Application daemon has started
        /// </summary>
        public event EventHandler Started;
        /// <summary>
        /// Application is stopping
        /// </summary>
        public event EventHandler Stopping;
        /// <summary>
        /// Application has stopped
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Add results to the query identifier
        /// </summary>
        public void AddResults(Guid queryId, IEnumerable<Guid> results)
        {
            try
            {
                var redisConn = this.m_connection.GetDatabase(RedisCacheConstants.QueryDatabaseId);
                if (redisConn.KeyExists($"{queryId}.{FIELD_QUERY_RESULT_IDX}"))
                {
                    redisConn.ListRightPush($"{queryId}.{FIELD_QUERY_RESULT_IDX}", results.Select(o => (RedisValue)o.ToByteArray()).ToArray());
                    var llength = redisConn.ListLength($"{queryId}.{FIELD_QUERY_RESULT_IDX}");
                    var totalCount = BitConverter.ToInt32(redisConn.StringGet($"{queryId}.{FIELD_QUERY_TOTAL_RESULTS}"), 0);

                    if (llength > totalCount)
                    {
                        redisConn.StringSet($"{queryId}.{FIELD_QUERY_TOTAL_RESULTS}",  BitConverter.GetBytes(totalCount + results.Count()));
                        redisConn.KeyExpire($"{queryId}.{FIELD_QUERY_TOTAL_RESULTS}", new TimeSpan(1, 0, 0));
                    }
                }
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error fetching results from REDIS: {0}", e);
                throw new Exception("Error fetching results from REDIS", e);
            }
        }

        /// <summary>
        /// Find query by identifier
        /// </summary>
        public Guid FindQueryId(object queryTag)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Get query results
        /// </summary>
        public IEnumerable<Guid> GetQueryResults(Guid queryId, int offset, int count)
        {
            try
            {
                var redisConn = this.m_connection.GetDatabase(RedisCacheConstants.QueryDatabaseId);
                redisConn.KeyExpire($"{queryId}.{FIELD_QUERY_RESULT_IDX}", new TimeSpan(1, 0, 0), CommandFlags.FireAndForget);
                redisConn.KeyExpire($"{queryId}.{FIELD_QUERY_TOTAL_RESULTS}", new TimeSpan(1, 0, 0), CommandFlags.FireAndForget);
                if (redisConn.KeyExists($"{queryId}.{FIELD_QUERY_RESULT_IDX}"))
                    return redisConn.ListRange($"{queryId}.{FIELD_QUERY_RESULT_IDX}", offset, offset + count).Select(o => new Guid((byte[])o)).ToArray();
                else
                    return new Guid[0];
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error fetching results from REDIS: {0}", e);
                throw new Exception("Error fetching results from REDIS", e);
            }
        }

        /// <summary>
        /// Gets the query tag
        /// </summary>
        public object GetQueryTag(Guid queryId)
        {
            try
            {
                var redisConn = this.m_connection.GetDatabase(RedisCacheConstants.QueryDatabaseId);
                return redisConn.StringGet($"{queryId}.{FIELD_QUERY_TAG_IDX}").ToString();
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error fetching: {0}", e);
                throw new Exception("Error fetching tag from REDIS", e);
            }
        }

        /// <summary>
        /// Determines if the query is registered
        /// </summary>
        public bool IsRegistered(Guid queryId)
        {
            try
            {
                var redisConn = this.m_connection.GetDatabase(RedisCacheConstants.QueryDatabaseId);
                return redisConn.KeyExists($"{queryId}.{FIELD_QUERY_TOTAL_RESULTS}");
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error fetching: {0}", e);
                throw new Exception("Error fetching from REDIS", e);
            }
        }

        /// <summary>
        /// Attempt to get the total result quantity
        /// </summary>
        public long QueryResultTotalQuantity(Guid queryId)
        {
            try
            {
                var redisConn = this.m_connection.GetDatabase(RedisCacheConstants.QueryDatabaseId);
                var strTotalCount = redisConn.StringGet($"{queryId}.{FIELD_QUERY_TOTAL_RESULTS}");
                if(strTotalCount.HasValue)
                    return BitConverter.ToInt32(strTotalCount, 0);
                return 0;
            }
            catch(Exception e)
            {
                this.m_tracer.TraceError("Error getting query result quantity: {0}", e);
                throw new Exception("Error getting query result from REDIS", e);
            }
        }

        /// <summary>
        /// Registers the specified query result 
        /// </summary>
        public bool RegisterQuerySet(Guid queryId, IEnumerable<Guid> results, object tag, int totalResults)
        {
            try
            {
                var redisConn = this.m_connection.GetDatabase(RedisCacheConstants.QueryDatabaseId);
                redisConn.KeyDelete($"{queryId}.{FIELD_QUERY_RESULT_IDX}");
                redisConn.ListRightPush($"{queryId}.{FIELD_QUERY_RESULT_IDX}", results.Select(o => (RedisValue)o.ToByteArray()).ToArray());
                if (tag != null)
                {
                    redisConn.StringSet($"{queryId}.{FIELD_QUERY_TAG_IDX}", tag.ToString());
                    redisConn.KeyExpire($"{queryId}.{FIELD_QUERY_TAG_IDX}", new TimeSpan(1, 0, 0));
                }
                redisConn.StringSet($"{queryId}.{FIELD_QUERY_TOTAL_RESULTS}", BitConverter.GetBytes(totalResults));
                redisConn.KeyExpire($"{queryId}.{FIELD_QUERY_RESULT_IDX}", new TimeSpan(1, 0, 0));
                redisConn.KeyExpire($"{queryId}.{FIELD_QUERY_TOTAL_RESULTS}", new TimeSpan(1, 0, 0));
                return true;
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error registering query in REDIS: {0}", e);
                throw new Exception("Error getting query result from REDIS", e);
            }
        }

        /// <summary>
        /// Sets the query tag if it exists
        /// </summary>
        public void SetQueryTag(Guid queryId, object value)
        {
            try
            {
                var redisConn = this.m_connection.GetDatabase(RedisCacheConstants.QueryDatabaseId);
                if (redisConn.KeyExists($"{queryId}.{FIELD_QUERY_RESULT_IDX}"))
                    redisConn.StringSet($"{queryId}.{FIELD_QUERY_TAG_IDX}", value?.ToString());
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error setting tags in REDIS: {0}", e);
                throw new Exception("Error setting query tag in REDIS", e);
            }
        }

        /// <summary>
        /// Start the daemon
        /// </summary>
        public bool Start()
        {
            try
            {
                this.Starting?.Invoke(this, EventArgs.Empty);

                this.m_tracer.TraceInfo("Starting REDIS query service to hosts {0}...", String.Join(";", this.m_configuration.Servers));

                var configuration = new ConfigurationOptions()
                {
                    Password = this.m_configuration.Password
                };
                foreach (var itm in this.m_configuration.Servers)
                    configuration.EndPoints.Add(itm);

                this.m_connection = ConnectionMultiplexer.Connect(configuration);
                
                this.Started?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error starting REDIS query persistence, will switch to query persister : {0}", e);
                ApplicationContext.Current.GetService<IServiceManager>().RemoveServiceProvider(typeof(RedisQueryPersistenceService));
                ApplicationContext.Current.GetService<IServiceManager>().RemoveServiceProvider(typeof(IDataCachingService));
                return false;
            }
        }

        /// <summary>
        /// Stops the connection broker
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
        /// Register query set
        /// </summary>
        /// <remarks>Backport from SanteDB</remarks>
        public bool RegisterQuerySet<TIdentifier>(string queryId, int count, Identifier<TIdentifier>[] results, object tag)
        {
            return this.RegisterQuerySet(Guid.Parse(queryId), results.OfType<Identifier<Guid>>().Select(o => o.Id), tag, count);
        }

        /// <summary>
        /// Add results
        /// </summary>
        /// <remarks>Backport from SanteDB</remarks>
        public bool AddResults<TIdentifier>(string queryId, Identifier<TIdentifier>[] results)
        {
            this.AddResults(Guid.Parse(queryId), results.OfType<Identifier<Guid>>().Select(o => o.Id));
            return true;
        }

        /// <summary>
        /// Return if query is registered
        /// </summary>
        /// <remarks>Backport from SanteDB</remarks>
        public bool IsRegistered(string queryId)
        {
            return this.IsRegistered(Guid.Parse(queryId));
        }

        /// <summary>
        /// Get specified query results
        /// </summary>
        /// <remarks>Backport from SanteDB</remarks>
        public Identifier<TIdentifier>[] GetQueryResults<TIdentifier>(string queryId, int startRecord, int nRecords)
        {

            return this.GetQueryResults(Guid.Parse(queryId), startRecord, nRecords).Select(o=>new Identifier<TIdentifier>((TIdentifier)(object)o)).ToArray();
        }

        /// <summary>
        /// Get the specified query tag
        /// </summary>
        /// <remarks>Backport from SanteDB</remarks>
        public object GetQueryTag(string queryId)
        {
            return this.GetQueryTag(Guid.Parse(queryId));
        }

        /// <summary>
        /// Get total results
        /// </summary>
        public long QueryResultTotalQuantity(string queryId)
        {
            return this.QueryResultTotalQuantity(Guid.Parse(queryId));
        }
    }
}
