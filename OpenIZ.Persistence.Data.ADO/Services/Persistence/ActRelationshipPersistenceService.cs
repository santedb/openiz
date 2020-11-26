/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: fyfej
 * Date: 2017-9-1
 */
using OpenIZ.Core.Model.Acts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using OpenIZ.OrmLite;
using System.Security.Principal;
using OpenIZ.Persistence.Data.ADO.Data.Model.Acts;
using OpenIZ.Persistence.Data.ADO.Data;
using OpenIZ.Core.Model.DataTypes;

namespace OpenIZ.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Represents a persister which is a act relationship 
    /// </summary>
    public class ActRelationshipPersistenceService : IdentifiedPersistenceService<ActRelationship, DbActRelationship>, IAdoAssociativePersistenceService
    {

        /// <summary>
        /// Get from source
        /// </summary>
        public IEnumerable GetFromSource(DataContext context, Guid id, decimal? versionSequenceId, IPrincipal principal)
        {
            int tr = 0;
            return this.QueryInternal(context, base.BuildSourceQuery<ActRelationship>(id, versionSequenceId), Guid.Empty, 0, null, out tr, principal, false).ToList();
        }

        /// <summary>
        /// Insert the relationship
        /// </summary>
        public override ActRelationship InsertInternal(DataContext context, ActRelationship data, IPrincipal principal)
        {
            // Ensure we haven't already persisted this
            if (data.TargetAct != null) data.TargetAct = data.TargetAct.EnsureExists(context, principal) as Act;
            data.TargetActKey = data.TargetAct?.Key ?? data.TargetActKey;
            data.RelationshipTypeKey = data.RelationshipType?.Key ?? data.RelationshipTypeKey;

            // Lookup the original 
            if (!data.EffectiveVersionSequenceId.HasValue)
                data.EffectiveVersionSequenceId = context.FirstOrDefault<DbActVersion>(o => o.Key == data.SourceEntityKey)?.VersionSequenceId;

            // Duplicate check 
            var existing = context.FirstOrDefault<DbActRelationship>(r => r.SourceKey == data.SourceEntityKey && r.TargetKey == data.TargetActKey && r.RelationshipTypeKey == data.RelationshipTypeKey && !r.ObsoleteVersionSequenceId.HasValue);
            if (existing == null)
                return base.InsertInternal(context, data, principal);
            else
                return this.ToModelInstance(existing, context, principal);
        }

        /// <summary>
        /// Update the specified object
        /// </summary>
        public override ActRelationship UpdateInternal(DataContext context, ActRelationship data, IPrincipal principal)
        {
            data.TargetActKey = data.TargetAct?.Key ?? data.TargetActKey;
            data.RelationshipTypeKey = data.RelationshipType?.Key ?? data.RelationshipTypeKey;

            if (data.ObsoleteVersionSequenceId == Int32.MaxValue)
                data.ObsoleteVersionSequenceId = data.SourceEntity?.VersionSequence ?? data.ObsoleteVersionSequenceId;

            // Duplicate check 
            var existing = context.FirstOrDefault<DbActRelationship>(r => r.SourceKey == data.SourceEntityKey && r.TargetKey == data.TargetActKey && r.RelationshipTypeKey == data.RelationshipTypeKey && !r.ObsoleteVersionSequenceId.HasValue);
            if (existing != null && existing.Key != data.Key) // There is an existing relationship which isn't this one, obsolete it 
            {
                existing.ObsoleteVersionSequenceId = data.SourceEntity?.VersionSequence;
                if (existing.ObsoleteVersionSequenceId.HasValue)
                    context.Update(existing);
                else
                {
                    this.m_tracer.TraceEvent(System.Diagnostics.TraceEventType.Warning, 9382, "ActRelationship {0} would conflict with existing {1} -> {2} (role {3}) already exists and this update would violate unique constraint.", data, existing.SourceKey, existing.TargetKey, existing.RelationshipTypeKey);
                    existing.ObsoleteVersionSequenceId = 1;
                    context.Update(existing);
                }
            }

            return base.UpdateInternal(context, data, principal);
        }

        /// <summary>
        /// Comparer for entity relationships
        /// </summary>
        internal class Comparer : IEqualityComparer<ActRelationship>
        {
            /// <summary>
            /// Determine equality between the two relationships
            /// </summary>
            public bool Equals(ActRelationship x, ActRelationship y)
            {
                return x.SourceEntityKey == y.SourceEntityKey &&
                    x.TargetActKey == y.TargetActKey &&
                    (x.RelationshipTypeKey == y.RelationshipTypeKey ||  x.RelationshipType?.Mnemonic == y.RelationshipType?.Mnemonic);
            }

            /// <summary>
            /// Get hash code
            /// </summary>
            public int GetHashCode(ActRelationship obj)
            {
                int result = obj.SourceEntityKey.GetHashCode();
                result = 37 * result + obj.RelationshipTypeKey.GetHashCode();
                result = 37 * result + obj.TargetActKey.GetHashCode();
                result = 37 * result + (obj.RelationshipType?.Mnemonic.GetHashCode() ?? 0);
                return result;
            }
        }
    }
}
