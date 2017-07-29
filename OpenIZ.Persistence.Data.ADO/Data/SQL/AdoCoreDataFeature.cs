﻿using MARC.HI.EHRS.SVC.Configuration.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Persistence.Data.ADO.Data.SQL
{
    /// <summary>
    /// Represents a data definition feature
    /// </summary>
    public class AdoCoreDataFeature : IDataFeature
    {
        /// <summary>
        /// Gets or sets the name of the feature
        /// </summary>
        public string Name
        {
            get
            {
                return "OpenIZ Core Deployment";
            }
        }

        /// <summary>
        /// Get the check sql
        /// </summary>
        public string GetCheckSql(string invariantName)
        {
            return "SELECT '';";
        }

        /// <summary>
        /// Get deployment sql
        /// </summary>
        public string GetDeploySql(string invariantName)
        {
            String[] resource = new String[]
            {
                "openiz-ddl.sql",
                "openiz-codes.sql",
                "openiz-fn.sql",
                "openiz-init.sql"
            };

            // Build sql
            switch (invariantName.ToLower())
            {
                case "npgsql":
                    StringBuilder sql = new StringBuilder();

                    foreach (var itm in resource)
                        using (var streamReader = new StreamReader(typeof(AdoCoreDataFeature).Assembly.GetManifestResourceStream($"OpenIZ.Persistence.Data.ADO.Data.SQL.PSQL.{itm}")))
                            sql.Append(streamReader.ReadToEnd());
                    return sql.ToString();
                default:
                    throw new InvalidOperationException($"Deployment for {invariantName} not supported");
            }
        }

        /// <summary>
        /// Get un-deploy sql
        /// </summary>
        public string GetUnDeploySql(string invariantName)
        {
            // Build sql
            switch (invariantName.ToLower())
            {
                case "npgsql":
                    using (var streamReader = new StreamReader(typeof(AdoCoreDataFeature).Assembly.GetManifestResourceStream($"OpenIZ.Persistence.Data.ADO.Data.SQL.PSQL.openiz-drop.sql")))
                        return streamReader.ReadToEnd();
                default:
                    throw new InvalidOperationException($"Deployment for {invariantName} not supported");
            }
        }
    }
}
