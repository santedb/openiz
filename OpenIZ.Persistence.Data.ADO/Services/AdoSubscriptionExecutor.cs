using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Data;
using MARC.HI.EHRS.SVC.Core.Event;
using MARC.HI.EHRS.SVC.Core.Services;
using OpenIZ.Core.Diagnostics;
using OpenIZ.Core.Exceptions;
using OpenIZ.Core.Model;
using OpenIZ.Core.Model.Interfaces;
using OpenIZ.Core.Model.Map;
using OpenIZ.Core.Model.Query;
using OpenIZ.Core.Model.Subscription;
using OpenIZ.Core.Security;
using OpenIZ.Core.Services;
using OpenIZ.OrmLite;
using OpenIZ.Persistence.Data.ADO.Data.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenIZ.Persistence.Data.ADO.Services
{
    /// <summary>
    /// Represents a default implementation of the subscription executor
    /// </summary>
    public class AdoSubscriptionExector : ISubscriptionExecutor
    {

        // Parameter regex
        private readonly Regex m_parmRegex = new Regex(@"(.*?)(\$\w*?\$)", RegexOptions.Multiline);

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "ADO.NET Subscription Executor";
        
        // Tracer
        private TraceSource m_tracer = new TraceSource(AdoDataConstants.TraceSourceName);

        /// <summary>
        /// Fired when the query is executed
        /// </summary>
        public event EventHandler<PostQueryEventArgs<IdentifiedData>> Executed;

        /// <summary>
        /// Fired when the query is about to execute
        /// </summary>
        public event EventHandler<PreQueryEventArgs<IdentifiedData>> Executing;

        /// <summary>
        /// Exectue the specified subscription
        /// </summary>
        public IEnumerable<object> Execute(Guid subscriptionKey, NameValueCollection parameters, int offset, int? count, out int totalResults, Guid queryId)
        {
            var subscription = ApplicationContext.Current.GetService<IRepositoryService<SubscriptionDefinition>>()?.Get(subscriptionKey);
            if (subscription == null)
                throw new KeyNotFoundException(subscriptionKey.ToString());
            else
                return this.Execute(subscription, parameters, offset, count, out totalResults, queryId);
        }

        /// <summary>
        /// Execute the current operation
        /// </summary>
        public IEnumerable<object> Execute(SubscriptionDefinition subscription, NameValueCollection parameters, int offset, int? count, out int totalResults, Guid queryId)
        {
            if (subscription == null || subscription.ServerDefinitions.Count == 0)
                throw new InvalidOperationException("Subscription does not have server definition");

            var preArgs = new PreQueryEventArgs<IdentifiedData>(o => o.Key == subscription.Key, AuthenticationContext.Current.Principal);
            this.Executing?.Invoke(this, preArgs);
            if (preArgs.Cancel)
            {
                this.m_tracer.TraceWarning("Pre-Event for executor failed");
                totalResults = 0;
                return new List<Object>();
            }

            var persistenceType = typeof(IDataPersistenceService<>).MakeGenericType(subscription.ResourceType);
            var persistenceInstance = ApplicationContext.Current.GetService(persistenceType) as IAdoPersistenceService;
            var queryService = ApplicationContext.Current.GetService<MARC.HI.EHRS.SVC.Core.Services.IQueryPersistenceService>();
            var cacheService = ApplicationContext.Current.GetService<IDataCachingService>();

            var provider = AdoPersistenceService.GetConfiguration().Provider;
            // Get the definition
            var definition = subscription.ServerDefinitions.FirstOrDefault(o => o.InvariantName == provider.Name);
            if (definition == null)
                throw new InvalidOperationException($"Subscription does not provide definition for provider {provider.Name}");

            // No obsoletion time?
            if (typeof(IBaseEntityData).IsAssignableFrom(subscription.ResourceType) && !parameters.ContainsKey("obsoletionTime"))
                parameters.Add("obsoletionTime", "null");

            // Query expression
            var queryExpression = typeof(QueryExpressionParser).GetGenericMethod(
                nameof(QueryExpressionParser.BuildLinqExpression),
                new Type[] { subscription.ResourceType },
                new Type[] { typeof(NameValueCollection) }
            ).Invoke(null, new object[] { parameters });

            var principal = AuthenticationContext.Current.Principal;

            // Query has been registered?
            if (queryId != Guid.Empty && queryService?.IsRegistered(queryId.ToString()) == true)
            {
                totalResults = (int)queryService.QueryResultTotalQuantity(queryId.ToString());
                return queryService.GetQueryResults<Guid>(queryId.ToString(), offset, count ?? 100)
                    .AsParallel()
                    .AsOrdered()
                    .Select(o =>
                    {
                        try
                        {
                            using (var ctx = provider.GetReadonlyConnection())
                            {
                                ctx.Open();
                                ctx.LoadState = LoadState.FullLoad;

                                var retVal = persistenceInstance.Get(ctx, (Guid)(object)o.Id, principal);
                                cacheService?.Add(retVal as IdentifiedData);
                                return retVal;
                            }
                        }
                        catch (Exception e)
                        {
                            this.m_tracer.TraceError("Error fetching query results for {0}: {1}", queryId, e);
                            throw new DataPersistenceException("Error fetching query results", e);
                        }
                    }).ToList();
            }
            else
            {
                // Now grab the context and query!!!
                using (var connection = provider.GetReadonlyConnection())
                {
                    try
                    {
                        connection.Open();
                        connection.LoadState = LoadState.FullLoad;
                        // First, build the query using the query build
                        var tableMapping = TableMapping.Get(AdoPersistenceService.GetMapper().MapModelType(subscription.ResourceType));
                        var query = (typeof(QueryBuilder).GetGenericMethod(
                            nameof(QueryBuilder.CreateQuery),
                            new Type[] { subscription.ResourceType },
                            new Type[] { queryExpression.GetType(), typeof(ColumnMapping).MakeArrayType() }
                        ).Invoke(AdoPersistenceService.GetQueryBuilder(), new object[] { queryExpression, null }) as SqlStatement).Build();

                        // Now we want to remove the portions of the built query statement after FROM and before WHERE as the definition will be the source of our selection
                        SqlStatement domainQuery = new SqlStatement(provider, query.SQL.Substring(0, query.SQL.IndexOf(" FROM ")));

                        // Append our query
                        var definitionQuery = definition.Definition;
                        List<Object> values = new List<object>();
                        definitionQuery = this.m_parmRegex.Replace(definitionQuery, (o) =>
                        {
                            var strValue = parameters["_" + o.Groups[2].Value.Substring(1, o.Groups[2].Value.Length - 2)].First();
                            Guid uuid = Guid.Empty;
                            if (Guid.TryParse(strValue, out uuid))
                                values.Add(uuid);
                            else
                                values.Add(strValue);
                            return o.Groups[1].Value + "?";
                        });

                        // Now we want to append 
                        domainQuery.Append(" FROM (").Append(definitionQuery, values.ToArray()).Append($") AS {tableMapping.TableName} ");
                        domainQuery.Append(query.SQL.Substring(query.SQL.IndexOf("WHERE ")), query.Arguments.ToArray());

                        // Now we want to create the result type
                        var resultType = tableMapping.OrmType;
                        if (typeof(IDbVersionedData).IsAssignableFrom(resultType)) // type is versioned so we have to join
                        {
                            var fkType = tableMapping.GetColumn("Key").ForeignKey.Table;
                            resultType = typeof(CompositeResult<,>).MakeGenericType(resultType, fkType);
                        }

                        // Now we want to select out our results
                        if (count == 0)
                        {
                            totalResults = connection.Count(domainQuery);
                            return null;
                        }
                        else
                        {
                            // Fetch
                            var domainResults = (typeof(DataContext).GetGenericMethod(
                                nameof(DataContext.Query),
                                new Type[] { resultType },
                                new Type[] { typeof(SqlStatement) }).Invoke(connection, new object[] { domainQuery }) as IEnumerable).OfType<Object>().ToList();

                            // Count
                            totalResults = domainResults.Count();

                            // Register query if query id specified
                            if (queryId != Guid.Empty)
                            {
                                if (typeof(CompositeResult).IsAssignableFrom(resultType))
                                    ApplicationContext.Current.GetService<MARC.HI.EHRS.SVC.Core.Services.IQueryPersistenceService>()?.RegisterQuerySet(queryId.ToString(), totalResults, domainResults.OfType<CompositeResult>().Select(o => new Identifier<Guid>((o.Values[0] as IDbIdentified).Key)).ToArray(), null);
                                else
                                    ApplicationContext.Current.GetService<MARC.HI.EHRS.SVC.Core.Services.IQueryPersistenceService>()?.RegisterQuerySet(queryId.ToString(), totalResults, domainResults.OfType<IDbIdentified>().Select(o => new Identifier<Guid>(o.Key)).ToArray(), null);

                            }

                            // Return
                            return domainResults
                                .Skip(offset)
                                .Take(count ?? 100)
                                .AsParallel()
                                .AsOrdered()
                                .Select(o =>
                                {
                                    try
                                    {
                                        using (var subConn = connection.OpenClonedContext())
                                        {
                                            var retVal = persistenceInstance.ToModelInstance(o, subConn, principal);
                                            cacheService?.Add(retVal as IdentifiedData);
                                            return retVal;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        this.m_tracer.TraceError("Error converting result: {0}", e);
                                        throw;
                                    }
                                }).ToList();

                        }

                        totalResults = 0;
                    }
                    catch (Exception e)
                    {

#if DEBUG
                        this.m_tracer.TraceError("Error executing subscription: {0}", e);
#else
                this.m_tracer.TraceError("Error executing subscription: {0}", e.Message);
#endif

                        throw new DataPersistenceException($"Error executing subscription: {e.Message}", e);
                    }
                } // using conn
            } // if


        }
    }
}
