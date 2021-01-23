using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Caching.Redis
{
    /// <summary>
    /// REDIS constants
    /// </summary>
    internal static class RedisCacheConstants
    {
        /// <summary>
        /// Trace source name
        /// </summary>
        public const string TraceSourceName = "SanteDB.Caching.Redis";

        /// <summary>
        /// Database ID for cache
        /// </summary>
        public const int CacheDatabaseId = 0;

        /// <summary>
        /// Database identifier
        /// </summary>
        public const int QueryDatabaseId = 1;

        /// <summary>
        /// Ad-hoc database
        /// </summary>
        public const int AdhocDatabaseId = 2;

    }
}

