﻿using OpenIZ.OrmLite.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Warehouse.ADO.Data.SQL.Model
{
    /// <summary>
    /// Represents an adhoc datamart
    /// </summary>
    [Table("adhoc_mart")]
    public class AdhocDatamart
    {

        /// <summary>
        /// Datamart identifier
        /// </summary>
        [Column("uuid"), NotNull, AutoGenerated]
        public Guid DatamartId { get; set; }

        /// <summary>
        /// Name of the datamart
        /// </summary>
        [Column("name"), NotNull]
        public String Name { get; set; }

        /// <summary>
        /// Creation time
        /// </summary>
        [Column("crt_utc"), NotNull, AutoGenerated]
        public DateTimeOffset CreationTime { get; set; }

        /// <summary>
        /// Schema identifier
        /// </summary>
        [Column("sch_uuid"), NotNull, ForeignKey(typeof(AdhocSchema), nameof(AdhocSchema.SchemaId))]
        public Guid SchemaId { get; set; }
    }
}