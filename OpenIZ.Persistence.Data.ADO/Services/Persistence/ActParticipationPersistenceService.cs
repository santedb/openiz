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
using OpenIZ.Core.Model.DataTypes;
using OpenIZ.Core.Model.Entities;
using OpenIZ.OrmLite;
using OpenIZ.Persistence.Data.ADO.Data;
using OpenIZ.Persistence.Data.ADO.Data.Model.Acts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using OpenIZ.Persistence.Data.ADO.Data.Model.Concepts;
using System.Diagnostics;

namespace OpenIZ.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Act participation persistence service
    /// </summary>
    public class ActParticipationPersistenceService : IdentifiedPersistenceService<ActParticipation, DbActParticipation, DbActParticipation>, IAdoAssociativePersistenceService
    {
        /// <summary>
        /// Get from source id
        /// </summary>
        public IEnumerable GetFromSource(DataContext context, Guid id, decimal? versionSequenceId, IPrincipal principal)
        {
            int tr = 0;
            return this.QueryInternal(context, base.BuildSourceQuery<ActParticipation>(id, versionSequenceId), Guid.Empty, 0, 1000, out tr, principal, false).ToList();

        }

        /// <summary>
        /// Append orderby
        /// </summary>
        protected override SqlStatement AppendOrderBy(SqlStatement rawQuery)
        {
            return rawQuery.OrderBy<DbActParticipation>(o => o.SequenceId);
        }

        /// <summary>
        /// Represents as a model instance
        /// </summary>
        public override ActParticipation ToModelInstance(object dataInstance, DataContext context, IPrincipal principal)
        {
            if (dataInstance == null) return null;

            var participationPart = dataInstance as DbActParticipation;
            var retVal = new ActParticipation()
            {
                EffectiveVersionSequenceId = participationPart.EffectiveVersionSequenceId,
                ObsoleteVersionSequenceId = participationPart.ObsoleteVersionSequenceId,
                ActKey = participationPart.SourceKey,
                PlayerEntityKey = participationPart.TargetKey,
                ParticipationRoleKey = participationPart.ParticipationRoleKey,
                LoadState = context.LoadState,
                Quantity = participationPart.Quantity,
                Key = participationPart.Key,
                SourceEntityKey = participationPart.SourceKey
            };

            if (context.LoadState == Core.Model.LoadState.FullLoad)
            {
                var concept = AdoPersistenceService.GetPersister(typeof(Concept)).Get(participationPart.ParticipationRoleKey);
                if (concept != null)
                    retVal.ParticipationRole = concept as Concept;
            }

            return retVal;
        }

        /// <summary>
        /// Insert the relationship
        /// </summary>
        public override ActParticipation InsertInternal(DataContext context, ActParticipation data, IPrincipal principal)
        {
            // Ensure we haven't already persisted this
            if (data.PlayerEntity != null) data.PlayerEntity = data.PlayerEntity.EnsureExists(context, principal) as Entity;
            data.PlayerEntityKey = data.PlayerEntity?.Key ?? data.PlayerEntityKey;
            data.ParticipationRoleKey = data.ParticipationRole?.Key ?? data.ParticipationRoleKey;
            if (data.Act != null) data.Act = data.Act.EnsureExists(context, principal) as Act;
            data.ActKey = data.Act?.Key ?? data.ActKey;

            // Lookup the original 
            if (!data.EffectiveVersionSequenceId.HasValue)
                data.EffectiveVersionSequenceId = context.FirstOrDefault<DbActVersion>(o => o.Key == data.SourceEntityKey)?.VersionSequenceId;

            // Duplicate check 
            var existing = context.FirstOrDefault<DbActParticipation>(r => r.SourceKey == data.SourceEntityKey && r.TargetKey == data.PlayerEntityKey && r.ParticipationRoleKey == data.ParticipationRoleKey && !r.ObsoleteVersionSequenceId.HasValue);
            if (existing == null)
                return base.InsertInternal(context, data, principal);
            else if (existing.Quantity != data.Quantity)
            {
                data.Key = existing.Key;
                return base.UpdateInternal(context, data, principal);
            }
            else
                return this.ToModelInstance(existing, context, principal);
        }

        /// <summary>
        /// Update the specified object
        /// </summary>
        public override ActParticipation UpdateInternal(DataContext context, ActParticipation data, IPrincipal principal)
        {
            data.PlayerEntityKey = data.PlayerEntity?.Key ?? data.PlayerEntityKey;
            data.ParticipationRoleKey = data.ParticipationRole?.Key ?? data.ParticipationRoleKey;
            data.ActKey = data.Act?.Key ?? data.ActKey;

            if (data.ObsoleteVersionSequenceId == Int32.MaxValue)
            {
                this.m_tracer.TraceEvent(TraceEventType.Warning, 9381, "ActParticipation {0} indicates obsolete", data);
                data.ObsoleteVersionSequenceId = data.SourceEntity?.VersionSequence ?? data.ObsoleteVersionSequenceId;
            }

            // Duplicate check 
            var existing = context.FirstOrDefault<DbActParticipation>(r => r.SourceKey == data.SourceEntityKey && r.TargetKey == data.PlayerEntityKey && r.ParticipationRoleKey == data.ParticipationRoleKey && !r.ObsoleteVersionSequenceId.HasValue);
            if (existing == null)
                return base.UpdateInternal(context, data, principal);
            else if (existing.Quantity != data.Quantity)
            {
                data.Key = existing.Key;
                return base.UpdateInternal(context, data, principal);
            }
            else
                return this.ToModelInstance(existing, context, principal);

        }

        /// <summary>
        /// Comparer for entity relationships
        /// </summary>
        internal class Comparer : IEqualityComparer<ActParticipation>
        {
            /// <summary>
            /// Determine equality between the two relationships
            /// </summary>
            public bool Equals(ActParticipation x, ActParticipation y)
            {
                return x.SourceEntityKey == y.SourceEntityKey &&
                    x.PlayerEntityKey == y.PlayerEntityKey &&
                    (x.ParticipationRoleKey == y.ParticipationRoleKey || x.ParticipationRole?.Mnemonic == y.ParticipationRole?.Mnemonic);
            }

            /// <summary>
            /// Get hash code
            /// </summary>
            public int GetHashCode(ActParticipation obj)
            {
                int result = obj.SourceEntityKey.GetHashCode();
                result = 37 * result + obj.PlayerEntityKey.GetHashCode();
                result = 37 * result + obj.ParticipationRoleKey.GetHashCode();
                result = 37 * result + (obj.ParticipationRole?.Mnemonic.GetHashCode() ?? 0);

                return result;
            }
        }
    }
}
