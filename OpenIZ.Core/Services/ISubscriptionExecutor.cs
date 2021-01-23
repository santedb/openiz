using MARC.HI.EHRS.SVC.Core.Event;
using OpenIZ.Core.Event;
using OpenIZ.Core.Model;
using OpenIZ.Core.Model.Query;
using OpenIZ.Core.Model.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Core.Services
{
    /// <summary>
    /// Represents a subscription executor
    /// </summary>
    public interface ISubscriptionExecutor
    {

        /// <summary>
        /// Occurs when queried.
        /// </summary>
        event EventHandler<PostQueryEventArgs<IdentifiedData>> Executed;

        /// <summary>
        /// Occurs when querying.
        /// </summary>
        event EventHandler<PreQueryEventArgs<IdentifiedData>> Executing;

        /// <summary>
        /// Executes the specified subscription mechanism
        /// </summary>
        /// <param name="subscriptionKey">The key of the subscription to run</param>
        /// <param name="parameters">The parameters from the query</param>
        /// <param name="offset">The start record</param>
        /// <param name="count">The number of records</param>
        /// <param name="totalResults">The total results in the subscription</param>
        /// <param name="queryId">The query identifier</param>
        /// <returns>The results from the execution</returns>
        IEnumerable<Object> Execute(Guid subscriptionKey, NameValueCollection parameters, int offset, int? count, out int totalResults, Guid queryId);

        /// <summary>
        /// Executes the provided subscription definition
        /// </summary>
        /// <param name="subscription">The loaded subscription definition to be used</param>
        /// <param name="parameters">The parameters to query</param>
        /// <param name="offset">The offset of the first record</param>
        /// <param name="count">The number of results</param>
        /// <param name="totalResults">The total matching results</param>
        /// <param name="queryId">A stateful query identifier</param>
        /// <returns>The results matching the filter parameters</returns>
        IEnumerable<Object> Execute(SubscriptionDefinition subscription, NameValueCollection parameters, int offset, int? count, out int totalResults, Guid queryId);
    }
}
