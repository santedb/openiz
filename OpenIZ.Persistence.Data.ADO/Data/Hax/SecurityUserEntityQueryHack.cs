using OpenIZ.Core.Model.Entities;
using OpenIZ.Core.Model.Security;
using OpenIZ.OrmLite;
using OpenIZ.Persistence.Data.ADO.Data.Model.Entities;
using OpenIZ.Persistence.Data.ADO.Data.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Persistence.Data.ADO.Data.Hax
{
    /// <summary>
    /// Query builder hack 
    /// </summary>
    public class SecurityUserEntityQueryHack : IQueryBuilderHack
    {
        /// <summary>
        /// Build a query based on ue
        /// </summary>
        public bool HackQuery(QueryBuilder builder, SqlStatement sqlStatement, SqlStatement whereClause, Type tmodel, PropertyInfo property, string queryPrefix, QueryPredicate predicate, object values, IEnumerable<TableMapping> scopedTables)
        {
            if (typeof(SecurityUser) == tmodel && property.Name == nameof(SecurityUser.UserEntity))
            {
                var userkey = TableMapping.Get(typeof(DbUserEntity)).GetColumn(nameof(DbUserEntity.SecurityUserKey), false);
                Dictionary<String, object> filter = new Dictionary<string, object>()
                {
                    {  predicate.SubPath, values }
                };

                var personSubSelect = builder.CreateQuery<UserEntity>(filter.ToArray(), userkey);
                var userIdKey = TableMapping.Get(typeof(DbSecurityUser)).PrimaryKey.FirstOrDefault();
                whereClause.And($"{userIdKey.Name} IN (").Append(personSubSelect).Append(")");
                return true;
            }
            return false;
        }
    }
}
