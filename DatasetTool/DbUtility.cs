using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using MohawkCollege.Util.Console.Parameters;
using OizDevTool.Model;
using OpenIZ.Core.Model.Entities;
using OpenIZ.Core.Model.Query;
using OpenIZ.Core.Security;
using OpenIZ.Core.Services;
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

namespace OizDevTool
{
    /// <summary>
    /// Database utility
    /// </summary>
    [Description("Tools for managing the OpenIZ Database")]
    public static class DbUtility
    {
        /// <summary>
        /// Archive database parameters
        /// </summary>
        public class ArchiveDatabaseParameters
        {

            /// <summary>
            /// Sets the rules file
            /// </summary>
            [Parameter("rules")]
            public string Rules { get; set; }
        }

        /// <summary>
        /// Trim all nullified objects
        /// </summary>
        public class TrimDatabaseParameters
        {

            /// <summary>
            /// Since date
            /// </summary>
            [Parameter("since")]
            public string Since { get; set; }
        }

        /// <summary>
        /// Archive database parameters
        /// </summary>
        [Description("Archives data according to the configured archiving rules")]
        [Example("Run all archive rules", "--rules=configuration.xml")]
        [ParameterClass(typeof(ArchiveDatabaseParameters))]
        public static void Archive(string[] args)
        {

            try
            {
                ApplicationContext.Current.Start();

                // Open rules file
                var parms = new ParameterParser<ArchiveDatabaseParameters>().Parse(args);
                XmlSerializer xsz = new XmlSerializer(typeof(DataRetentionConfiguration));

                DataRetentionConfiguration configuration = null;
                // No rules? Use default
                if (String.IsNullOrEmpty(parms.Rules))
                    parms.Rules = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "retention.config");
                using (var fs = File.OpenRead(parms.Rules))
                    configuration = xsz.Deserialize(fs) as DataRetentionConfiguration;

                // Parameters
                Dictionary<String, Delegate> parameters = new Dictionary<string, Delegate>();
                foreach (var vardef in configuration.Variables)
                    parameters.Add(vardef.Name, vardef.CompileFunc());

                // Copy over the specified security roles, etc.
                foreach (var rule in configuration.RetentionRules)
                {

                    Console.WriteLine("Running retention rule {0} ({1} {2})", rule.Name, rule.Action, rule.ResourceTypeXml);

                    var pserviceType = typeof(IDataPersistenceService<>).MakeGenericType(rule.ResourceType);
                    var persistenceService = ApplicationContext.Current.GetService(pserviceType) as IBulkDataPersistenceService;
                    if (persistenceService == null)
                        throw new InvalidOperationException("Cannot locate appropriate persistence service");

                    // Included keys for retention
                    IEnumerable<Guid> keys = new Guid[0];
                    foreach (var exprDef in rule.IncludeExpressions)
                    {

                        var expr = QueryExpressionParser.BuildLinqExpression(rule.ResourceType, NameValueCollection.ParseQueryString(exprDef), parameters);
                        int offset = 0, totalCount = 1;
                        Console.WriteLine("\t Including {0}", new NameValueCollection(QueryExpressionBuilder.BuildQuery(rule.ResourceType, expr).ToArray()).ToString());

                        while (offset < totalCount) // gather the included keys
                        {
                            keys = keys.Union(persistenceService.QueryKeys(expr, offset, 1000, out totalCount));
                            offset += 1000;
                        }
                    }

                    // Exclude keys from retention
                    foreach (var exprDef in rule.ExcludeExpressions)
                    {
                        var expr = QueryExpressionParser.BuildLinqExpression(rule.ResourceType, NameValueCollection.ParseQueryString(exprDef), parameters);
                        int offset = 0, totalCount = 1;
                        while (offset < totalCount) // gather the included keys
                        {
                            Console.WriteLine("\t Excluding {0}", expr);

                            keys = keys.Except(persistenceService.QueryKeys(expr, offset, 1000, out totalCount));
                            offset += 1000;

                        }
                    }

                    keys = keys.Distinct();
                    Console.WriteLine($"Executing {rule.Action} {rule.ResourceTypeXml} ({rule.Name}) on {keys.Count()} objects");

                    if (persistenceService is IReportProgressChanged rpc)
                        rpc.ProgressChanged += (o, e) => Console.WriteLine("{0} - {1:0%}", e.State, e.Progress);

                    if (keys.Any(k => k.ToString() == "ed144bd2-a334-40a2-9a8f-b767a1397d07"))
                        System.Diagnostics.Debugger.Break();

                    // Now we want to execute the specified action
                    switch (rule.Action)
                    {
                        case DataRetentionActionType.Obsolete:
                            persistenceService.Obsolete(TransactionMode.Commit, AuthenticationContext.SystemPrincipal, keys.ToArray());
                            break;
                        case DataRetentionActionType.Purge:
                            persistenceService.Purge(TransactionMode.Commit, AuthenticationContext.SystemPrincipal, keys.ToArray());
                            break;
                        case DataRetentionActionType.Archive:
                        case DataRetentionActionType.Archive | DataRetentionActionType.Obsolete:
                        case DataRetentionActionType.Archive | DataRetentionActionType.Purge:
                            var archiveService = ApplicationContext.Current.GetService<IDataArchiveService>();
                            if (archiveService == null)
                                throw new InvalidOperationException("Could not find archival service");
                            // Test PURGE
                            if (rule.Action.HasFlag(DataRetentionActionType.Purge))
                            {
                                //persistenceService.Purge(TransactionMode.Rollback, AuthenticationContext.SystemPrincipal, keys.ToArray());
                                archiveService.Archive(rule.ResourceType, keys.ToArray());
                                persistenceService.Purge(TransactionMode.Commit, AuthenticationContext.SystemPrincipal, keys.ToArray());
                            }
                            else if (rule.Action.HasFlag(DataRetentionActionType.Obsolete))
                            {
                                archiveService.Archive(rule.ResourceType, keys.ToArray());
                                persistenceService.Obsolete(TransactionMode.Commit, AuthenticationContext.SystemPrincipal, keys.ToArray());
                            }
                            else
                            {
                                archiveService.Archive(rule.ResourceType, keys.ToArray());
                            }
                            break;
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR Running Archive: {0}", e);
                //EventLog.WriteEntry("OPENIZ", $"Error running archive tool - {e}");
                Environment.Exit(-1);
            }
        }

        /// <summary>
        /// Remove / perform squash DB
        /// </summary>
        [Description("Removes all historical versions from the database")]
        public static void Squash(string[] args)
        {
            var dbProvider = ApplicationContext.Current.GetService<ISqlDataPersistenceService>();
            Console.WriteLine("Fixing references...");
            dbProvider.ExecuteNonQuery("SELECT fix_duplicates()");
            Console.WriteLine("Squashing database (note: this may take up to 3 hours)");
            dbProvider.ExecuteNonQuery("SELECT squash_db()");
        }

        /// <summary>
        /// Remove all nullable objects
        /// </summary>
        [Description("Removes all objects from the database which are marked NULLIFIED before the specified date")]
        [ParameterClass(typeof(TrimDatabaseParameters))]
        public static void Trim(String[] args)
        {
            var parms = new ParameterParser<TrimDatabaseParameters>().Parse(args);
            var dbProvider = ApplicationContext.Current.GetService<ISqlDataPersistenceService>();
            Console.WriteLine("Fixing references...");
            dbProvider.ExecuteNonQuery($"SELECT del_nullified('{parms.Since}')");

        }
    }
}
