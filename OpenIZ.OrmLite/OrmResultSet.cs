using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.OrmLite
{

    /// <summary>
    /// Non-generic interface
    /// </summary>
    public interface IOrmResultSet : IEnumerable, IDisposable
    {

        /// <summary>
        /// Counts the number of records
        /// </summary>
        int Count();

        /// <summary>
        /// Skip N results
        /// </summary>
        IOrmResultSet Skip(int count);

        /// <summary>
        /// Take N results
        /// </summary>
        IOrmResultSet Take(int count);

        /// <summary>
        /// Gets the specified key
        /// </summary>
        IOrmResultSet Keys<TKey>();

        /// <summary>
        /// Clones the object on a new context standalone from the creation context
        /// </summary>
        IOrmResultSet AsStandalone();
    }

    /// <summary>
    /// Represents a result set from enumerable data
    /// </summary>
    /// <typeparam name="TData">The type of record this result set holds</typeparam>
    public class OrmResultSet<TData> : IEnumerable<TData>, IOrmResultSet
    {

        /// <summary>
        /// Gets the SQL statement that this result set is based on
        /// </summary>
        public SqlStatement Statement { get; private set; }

        /// <summary>
        /// Get the context
        /// </summary>
        public DataContext Context { get; private set; }

        /// <summary>
        /// Create a new result set based on the context and statement
        /// </summary>
        /// <param name="stmt">The SQL Statement</param>
        /// <param name="context">The data context on which data should be executed</param>
        internal OrmResultSet(DataContext context, SqlStatement stmt)
        {
            this.Statement = stmt;
            this.Context = context;
        }

        /// <summary>
        /// Instructs the reader to skip n records
        /// </summary>
        public OrmResultSet<TData> Skip(int n)
        {
            return new OrmResultSet<TData>(this.Context, this.Statement.Build().Offset(n));
        }

        /// <summary>
        /// Instructs the reader to take <paramref name="n"/> records
        /// </summary>
        /// <param name="n">The number of records to take</param>
        public OrmResultSet<TData> Take(int n)
        {
            return new OrmResultSet<TData>(this.Context, this.Statement.Build().Limit(n));
        }

        /// <summary>
        /// Instructs the reader to count the number of records
        /// </summary>
        public int Count()
        {
            return this.Context.Count(this.Statement);
        }

        /// <summary>
        /// Select distinct records
        /// </summary>
        public OrmResultSet<TData> Distinct()
        {
            var innerQuery = this.Statement.Build();
            if (!this.Statement.SQL.StartsWith("SELECT DISTINCT"))
                return new OrmResultSet<TData>(this.Context, this.Context.CreateSqlStatement($"SELECT DISTINCT {innerQuery.SQL.Substring(7)}", innerQuery.Arguments.ToArray()));
            else
                return this;
        }

        /// <summary>
        /// Get member information from lambda
        /// </summary>
        protected MemberInfo GetMember(Expression expression)
        {
            if (expression is MemberExpression) return (expression as MemberExpression).Member;
            else if (expression is UnaryExpression) return this.GetMember((expression as UnaryExpression).Operand);
            else throw new InvalidOperationException($"{expression} not supported, please use a member access expression");
        }

        /// <summary>
        /// Select the specified column
        /// </summary>
        public OrmResultSet<T> Select<T>(Expression<Func<TData, T>> column)
        {
            var mapping = TableMapping.Get(typeof(TData)).GetColumn(this.GetMember(column.Body));
            return new OrmResultSet<T>(this.Context, this.Context.CreateSqlStatement($"SELECT {mapping.Name} FROM (").Append(this.Statement).Append(") AS I"));
        }

        /// <summary>
        /// Get the maximum value of the specifed column
        /// </summary>
        public T Max<T>(Expression<Func<TData, T>> column)
        {
            var mapping = TableMapping.Get(typeof(TData)).GetColumn(this.GetMember(column.Body));
            return this.Context.ExecuteScalar<T>(this.Context.CreateSqlStatement($"SELECT MAX({mapping.Name}) FROM (").Append(this.Statement).Append(") AS I"));
        }

        /// <summary>
        /// Get the maximum value of the specifed column
        /// </summary>
        public T Min<T>(Expression<Func<TData, T>> column)
        {
            var mapping = TableMapping.Get(typeof(TData)).GetColumn(this.GetMember(column.Body));
            return this.Context.ExecuteScalar<T>(this.Context.CreateSqlStatement($"SELECT MIN({mapping.Name}) FROM (").Append(this.Statement).Append(") AS I"));
        }

        /// <summary>
        /// Select the specified column
        /// </summary>
        public OrmResultSet<dynamic> Select(params Expression<Func<TData, dynamic>>[] columns)
        {
            var mapping = TableMapping.Get(typeof(TData));
            return new OrmResultSet<dynamic>(this.Context, this.Context.CreateSqlStatement($"SELECT {String.Join(",", columns.Select(o => mapping.GetColumn(this.GetMember(o.Body))).Select(o => o.Name))} FROM (").Append(this.Statement).Append(") AS I"));
        }

        /// <summary>
        /// Instructs the reader to order by specified records
        /// </summary>
        /// <param name="selector">The key to order by</param>
        public OrmResultSet<TData> OrderBy(Expression<Func<TData, dynamic>> keySelector)
        {
            return new OrmResultSet<TData>(this.Context, this.Statement.Build().OrderBy<TData>(keySelector, Core.Model.Map.SortOrderType.OrderBy));
        }

        /// <summary>
        /// Instructs the reader to order by specified records
        /// </summary>
        /// <param name="selector">The selector to order by </param>
        public OrmResultSet<TData> OrderByDescending(Expression<Func<TData, dynamic>> keySelector)
        {
            return new OrmResultSet<TData>(this.Context, this.Statement.Build().OrderBy<TData>(keySelector, Core.Model.Map.SortOrderType.OrderByDescending));
        }

        /// <summary>
        /// Return the first object
        /// </summary>
        public TData First()
        {
            TData retVal = this.FirstOrDefault();
            if (retVal == null)
                throw new InvalidOperationException("Sequence contains no elements");
            return retVal;
        }

        /// <summary>
        /// Return the first object
        /// </summary>
        public TData FirstOrDefault()
        {
            return this.Context.FirstOrDefault<TData>(this.Statement);
        }

        /// <summary>
        /// Get the enumerator
        /// </summary>
        public IEnumerator<TData> GetEnumerator()
        {
            return this.Context.ExecQuery<TData>(this.Statement).GetEnumerator();
        }

        /// <summary>
        /// Get the non-generic enumerator
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Context.ExecQuery<TData>(this.Statement).GetEnumerator();
        }

        /// <summary>
        /// Gets the specified keys from the object
        /// </summary>
        public OrmResultSet<T> Keys<T>(bool qualifyKeyTableName = true)
        {

            var innerQuery = this.Statement.Build();
            var tm = TableMapping.Get(typeof(TData));
            if (tm.TableName.StartsWith("CompositeResult"))
            {
                tm = TableMapping.Get(typeof(TData).GetGenericArguments().Last());
            }
            if (tm.PrimaryKey.Count() != 1)
                throw new InvalidOperationException("Cannot execute KEY query on object with no keys");

            // HACK: Swap out SELECT * if query starts with it
            if (innerQuery.SQL.StartsWith("SELECT * "))
            {
                if(qualifyKeyTableName)
                    innerQuery = this.Context.CreateSqlStatement($"SELECT {tm.TableName}.{tm.PrimaryKey.First().Name} {innerQuery.SQL.Substring(9)}", innerQuery.Arguments.ToArray());
                else
                    innerQuery = this.Context.CreateSqlStatement($"SELECT {tm.PrimaryKey.First().Name} {innerQuery.SQL.Substring(9)}", innerQuery.Arguments.ToArray());
            }

            return new OrmResultSet<T>(this.Context, this.Context.CreateSqlStatement($"SELECT {String.Join(",", tm.PrimaryKey.Select(o => o.Name))} FROM (").Append(innerQuery).Append(") AS I"));

        }

        /// <summary>
        /// Gets the specified keys from the specified object type
        /// </summary>
        public IEnumerable<T> Keys<TKeyTable, T>()
        {
            var innerQuery = this.Statement.Build();
            var tm = TableMapping.Get(typeof(TKeyTable));

            if (tm.PrimaryKey.Count() != 1)
                throw new InvalidOperationException("Cannot execute KEY query on object with no keys");

            // HACK: Swap out SELECT * if query starts with it
            if (innerQuery.SQL.StartsWith("SELECT * "))
                innerQuery = this.Context.CreateSqlStatement($"SELECT {tm.TableName}.{tm.PrimaryKey.First().Name} {innerQuery.SQL.Substring(9)}", innerQuery.Arguments.ToArray());


            return new OrmResultSet<T>(this.Context, this.Context.CreateSqlStatement($"SELECT {String.Join(",", tm.PrimaryKey.Select(o => o.Name))} FROM (").Append(innerQuery).Append(") AS I"));
        }

        /// <summary>
        /// Gets the keys
        /// </summary>
        IOrmResultSet IOrmResultSet.Keys<TKey>()
        {
            return this.Keys<TKey>(false);
        }

        /// <summary>
        /// Gets the keys
        /// </summary>
        IOrmResultSet IOrmResultSet.Skip(int n)
        {
            return this.Skip(n);
        }

        /// <summary>
        /// Gets the keys
        /// </summary>
        IOrmResultSet IOrmResultSet.Take(int n)
        {
            return this.Take(n);
        }

        /// <summary>
        /// Swap the data context
        /// </summary>
        public IOrmResultSet AsStandalone()
        {
            return new OrmResultSet<TData>(this.Context.OpenClonedContext(), this.Statement);
        }

        /// <summary>
        /// Dispose this object
        /// </summary>
        public void Dispose()
        {
            this.Context?.Dispose();
        }
    }
}
