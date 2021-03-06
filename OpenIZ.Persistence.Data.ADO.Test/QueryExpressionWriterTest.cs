﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenIZ.Persistence.Data.ADO.Data.Model.Acts;
using System.Linq;
using System.Diagnostics;
using OpenIZ.Core.Model.Acts;
using OpenIZ.Core.Model.Roles;
using OpenIZ.Core.Model.Constants;
using OpenIZ.Core.Model.Security;
using OpenIZ.Core.Model.Entities;
using OpenIZ.OrmLite;
using OpenIZ.Core.Model.Map;
using OpenIZ.Persistence.Data.ADO.Services;
using OpenIZ.OrmLite.Providers;

namespace OpenIZ.Persistence.Data.ADO.Test
{
    [TestClass]
    public class QueryExpressionWriterTest
    {

        private QueryBuilder m_builder = new QueryBuilder(new ModelMapper(typeof(AdoPersistenceService).Assembly.GetManifestResourceStream(AdoDataConstants.MapResourceName)), new PostgreSQLProvider());

        /// <summary>
        /// Test that the function constructs an empty select statement
        /// </summary>
        [TestMethod]
        public void TestConstructsEmptySelectStatement()
        {
            SqlStatement sql = new SqlStatement<DbAct>(new PostgreSQLProvider()).SelectFrom("foo").Build();
            Assert.IsTrue(sql.SQL.Contains("AS foo"));
            Assert.IsTrue(sql.SQL.Contains("SELECT * FROM act_tbl"));
            Assert.AreEqual(0, sql.Arguments.Count());
        }

        /// <summary>
        /// Tests that the function constructs parameters
        /// </summary>
        [TestMethod]
        public void TestConstructsParameters()
        {
            SqlStatement sql = new SqlStatement<DbAct>(new PostgreSQLProvider()).SelectFrom("foo").Where("act_id = ?", Guid.NewGuid()).Build();
            Assert.IsTrue(sql.SQL.Contains("AS foo"));
            Assert.IsTrue(sql.SQL.Contains("SELECT * FROM act_tbl"));
            Assert.AreEqual(1, sql.Arguments.Count());
        }


        /// <summary>
        /// Tests that the function constructs parameters
        /// </summary>
        [TestMethod]
        public void TestConstructLocalParameters()
        {
            SqlStatement sql = new SqlStatement<DbActVersion>(new PostgreSQLProvider()).SelectFrom("foo").Where("act_id = ?", Guid.NewGuid()).And("act_utc < ?", DateTime.Now).Build();
            Assert.IsTrue(sql.SQL.Contains("AS foo"));
            Assert.IsTrue(sql.SQL.Contains("AND"));
            Assert.IsTrue(sql.SQL.Contains("act_id"));
            Assert.IsTrue(sql.SQL.Contains("act_utc"));
            Assert.IsTrue(sql.SQL.Contains("SELECT * FROM act_vrsn_tbl"));
            Assert.AreEqual(2, sql.Arguments.Count());
            Assert.IsInstanceOfType(sql.Arguments.First(), typeof(Guid));
            Assert.IsInstanceOfType(sql.Arguments.Last(), typeof(DateTime));
        }

        /// <summary>
        /// Test re-writing of simple LINQ
        /// </summary>
        [TestMethod]
        public void TestRewriteSimpleLinq()
        {
            Guid mg = Guid.NewGuid();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            SqlStatement sql = new SqlStatement<DbActVersion>(new PostgreSQLProvider()).SelectFrom("foo").Where(o => o.IsNegated == true).Build();
            sw.Stop();
            Assert.IsTrue(sql.SQL.Contains("AS foo"));
            Assert.IsTrue(sql.SQL.Contains("neg_ind =  ?"));
            Assert.AreEqual(1, sql.Arguments.Count());

        }

        /// <summary>
        /// Test re-writing of simple LINQ
        /// </summary>
        [TestMethod]
        public void TestRewriteComplexLinq()
        {
            Guid mg = Guid.NewGuid();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            SqlStatement sql = new SqlStatement<DbActVersion>(new PostgreSQLProvider()).SelectFrom("foo").Where(o => o.Key == mg || o.Key == Guid.NewGuid() && o.CreationTime <= DateTime.Now).Build();
            sw.Stop();

            Assert.IsTrue(sql.SQL.Contains("AS foo"));
            Assert.IsTrue(sql.SQL.Contains("AND"));
            Assert.IsTrue(sql.SQL.Contains("act_id"));
            Assert.IsTrue(sql.SQL.Contains("crt_utc"));
            Assert.IsTrue(sql.SQL.Contains("SELECT * FROM act_vrsn_tbl"));
            Assert.AreEqual(1, sql.Arguments.Count());
            Assert.IsInstanceOfType(sql.Arguments.First(), typeof(Guid));
        }

        /// <summary>
        /// Test re
        /// </summary>
        [TestMethod]
        public void TestModelQueryShouldJoin()
        {

            var query = m_builder.CreateQuery<Patient>(o => o.DeterminerConceptKey == DeterminerKeys.Specific).Build();
            Assert.IsTrue(query.SQL.Contains("SELECT * FROM pat_tbl"));
            Assert.IsTrue(query.SQL.Contains("INNER JOIN ent_vrsn_tbl"));

        }

        /// <summary>
        /// Test re
        /// </summary>
        [TestMethod]
        public void TestModelQueryShouldExistsClause()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var query = m_builder.CreateQuery<Patient>(o => o.DeterminerConcept.Mnemonic == "Instance").Build();
            sw.Stop();

            Assert.IsTrue(query.SQL.Contains("SELECT * FROM pat_tbl"));
            Assert.IsTrue(query.SQL.Contains("INNER JOIN ent_vrsn_tbl"));
            Assert.IsTrue(query.SQL.Contains("WITH"));
            Assert.IsTrue(query.SQL.Contains("cte0"));
            Assert.AreEqual(1, query.Arguments.Count());
        }

        /// <summary>
        /// Test re
        /// </summary>
        [TestMethod]
        public void TestQueryShouldWriteNestedJoin()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var query = m_builder.CreateQuery<Entity>(o => o.Identifiers.Any(i => i.Value == "123" && i.Authority.Oid == "1.2.3.4.6.7.8.9" || i.Value == "321" && i.Authority.Oid == "1.2.3.4.5.6.7.8") && o.ClassConceptKey == EntityClassKeys.Organization);
            sw.Stop();

            Assert.IsTrue(query.SQL.Contains("SELECT * FROM pat_tbl"));
            Assert.IsTrue(query.SQL.Contains("INNER JOIN ent_vrsn_tbl"));
            Assert.IsTrue(query.SQL.Contains("WITH"));
            Assert.IsTrue(query.SQL.Contains("cte0"));
            Assert.AreEqual(1, query.Arguments.Count());
        }

        /// <summary>
        /// Test re
        /// </summary>
        [TestMethod]
        public void TestModelQueryShouldSubQueryClause()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var query = m_builder.CreateQuery<Patient>(o => o.Participations.Where(guard => guard.ParticipationRole.Mnemonic == "RecordTarget").Any(sub => sub.PlayerEntity.ObsoletionTime == null));
            sw.Stop();

            Assert.IsTrue(query.SQL.Contains("SELECT * FROM pat_tbl"));
            Assert.IsTrue(query.SQL.Contains("INNER JOIN ent_vrsn_tbl"));
            Assert.IsTrue(query.SQL.Contains("WITH"));
            Assert.IsTrue(query.SQL.Contains("cte0"));
            Assert.AreEqual(1, query.Arguments.Count());
        }

        /// <summary>
        /// Test re
        /// </summary>
        [TestMethod]
        public void TestModelQueryShouldSubQueryIntersect()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var query = m_builder.CreateQuery<Entity>(o => o.Names.Where(guard => guard.NameUse.Mnemonic == "Legal").Any(v => v.Component.Where(b => b.ComponentType.Mnemonic == "Family").Any(n => n.Value == "Smith")) &&
                o.Names.Where(guard => guard.NameUse.Mnemonic == "Legal").Any(v => v.Component.Where(b => b.ComponentType.Mnemonic == "Given").Any(n => n.Value == "John" || n.Value == "Jacob"))).Build();
            sw.Stop();

            Assert.IsTrue(query.SQL.Contains("SELECT * FROM ent_vrsn_tbl"));
            Assert.IsTrue(query.SQL.Contains("INNER JOIN ent_tbl"));
            Assert.IsTrue(query.SQL.Contains("WITH"));
            Assert.IsTrue(query.SQL.Contains("cte0"));
            Assert.AreEqual(1, query.Arguments.Count());
        }

        /// <summary>
        /// Tests that the query writer works properly when querying based on a property that is non-serialized
        /// </summary>
        [TestMethod]
        public void TestModelQueryShouldUseNonSerialized()
        {
            var query = m_builder.CreateQuery<Entity>(o => o.Extensions.Any(e => e.ExtensionDisplay == "1"));
            Assert.IsTrue(query.SQL.Contains("SELECT * FROM ent_vrsn_tbl"));
            Assert.IsTrue(query.SQL.Contains("INNER JOIN ent_tbl"));
            Assert.IsTrue(query.SQL.Contains("WITH"));
            Assert.IsTrue(query.SQL.Contains("ext_disp"));
            Assert.AreEqual(1, query.Arguments.Count());
        }
    }
}
