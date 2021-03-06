﻿using MARC.HI.EHRS.SVC.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Core.Services
{
    /// <summary>
    /// Represents a data persisetence service that can handle bulk operations
    /// </summary>
    public interface IBulkDataPersistenceService
    {

        /// <summary>
        /// Obsolete the specified data
        /// </summary>
        void Obsolete(TransactionMode transactionMode, IPrincipal principal, params Guid[] keysToObsolete);

        /// <summary>
        /// Purge the specified data (erase it)
        /// </summary>
        void Purge(TransactionMode transactionMode, IPrincipal principal, params Guid[] keysToPurge);

        /// <summary>
        /// Query only for keys based on the expression (do not load objects from database)
        /// </summary>
        IEnumerable<Guid> QueryKeys(Expression query, int offset, int? count, out int totalResults);

    }
}
