﻿/*
 * Copyright 2015-2017 Mohawk College of Applied Arts and Technology
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
 * User: justi
 * Date: 2016-6-18
 */
using MARC.HI.EHRS.SVC.Core;
using OpenIZ.Core.Model.Constants;
using OpenIZ.Core.Model.Entities;
using OpenIZ.Core.Services;
using OpenIZ.Persistence.Data.ADO.Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using OpenIZ.Persistence.Data.ADO.Data;
using OpenIZ.Persistence.Data.ADO.Data.Model.Entities;
using OpenIZ.Persistence.Data.ADO.Data.Model.DataType;
using OpenIZ.Core.Model.DataTypes;
using System.Collections;
using OpenIZ.OrmLite;
using OpenIZ.Core.Interfaces;

namespace OpenIZ.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Entity name persistence service
    /// </summary>
    public class EntityNamePersistenceService : IdentifiedPersistenceService<Core.Model.Entities.EntityName, DbEntityName>, IAdoAssociativePersistenceService
    {
        /// <summary>
        /// Get from source
        /// </summary>
        public IEnumerable GetFromSource(DataContext context, Guid id, decimal? versionSequenceId, IPrincipal principal)
        {
            int tr = 0;
            return this.QueryInternal(context, base.BuildSourceQuery<EntityName>(id, versionSequenceId), Guid.Empty, 0, null, out tr, principal, false);
        }

        /// <summary>
        /// Insert the specified object
        /// </summary>
        public override Core.Model.Entities.EntityName InsertInternal(DataContext context, Core.Model.Entities.EntityName data, IPrincipal principal)
        {
            // Ensure exists
            if (data.NameUse != null) data.NameUse = data.NameUse?.EnsureExists(context, principal) as Concept;
            data.NameUseKey = data.NameUse?.Key ?? data.NameUseKey;
            var retVal = base.InsertInternal(context, data, principal);

            // Data component
            if (data.Component != null)
                base.UpdateAssociatedItems<Core.Model.Entities.EntityNameComponent, DbEntityNameComponent>(
                   data.Component,
                    data,
                    context,
                    principal);

            return retVal;
        }

        /// <summary>
        /// Update the entity name
        /// </summary>
        public override Core.Model.Entities.EntityName UpdateInternal(DataContext context, Core.Model.Entities.EntityName data, IPrincipal principal)
        {
            // Ensure exists
            if (data.NameUse != null) data.NameUse = data.NameUse?.EnsureExists(context, principal) as Concept;

            data.NameUseKey = data.NameUse?.Key ?? data.NameUseKey;

            var retVal = base.UpdateInternal(context, data, principal);

            var sourceKey = data.Key.Value.ToByteArray();

            // Data component
            if (data.Component != null)
                base.UpdateAssociatedItems<Core.Model.Entities.EntityNameComponent, DbEntityNameComponent>(
                   data.Component,
                    data,
                    context,
                    principal);

            return retVal;
        }

    }

    /// <summary>
    /// Represents an entity name component persistence service
    /// </summary>
    public class EntityNameComponentPersistenceService : IdentifiedPersistenceService<Core.Model.Entities.EntityNameComponent, DbEntityNameComponent, CompositeResult<DbEntityNameComponent, DbPhoneticValue>>, IAdoAssociativePersistenceService
    {
        /// <summary>
        /// From model instance
        /// </summary>
        public override object FromModelInstance(Core.Model.Entities.EntityNameComponent modelInstance, DataContext context, IPrincipal princpal)
        {
            var retVal = base.FromModelInstance(modelInstance, context, princpal) as DbEntityNameComponent;

            // Duplicate name?
            var existing = context.FirstOrDefault<DbPhoneticValue>(o => o.Value == modelInstance.Value);
            if (existing != null && existing.Key != retVal.ValueKey)
                retVal.ValueKey = existing.Key;
            else if (existing == null)
            {
                var phoneticCoder = ApplicationContext.Current.GetService<IPhoneticAlgorithmHandler>();
                retVal.ValueKey = context.Insert(new DbPhoneticValue()
                {
                    Value = modelInstance.Value,
                    PhoneticAlgorithmKey = phoneticCoder?.AlgorithmId ?? PhoneticAlgorithmKeys.None,
                    PhoneticCode = phoneticCoder?.GenerateCode(modelInstance.Value)
                }).Key;
            }

            return retVal;
        }

        /// <summary>
        /// Convert to model instance
        /// </summary>
        public override EntityNameComponent ToModelInstance(object dataInstance, DataContext context, IPrincipal principal)
        {
            if (dataInstance == null) return null;

            var nameComp = (dataInstance as CompositeResult)?.Values.OfType<DbEntityNameComponent>().FirstOrDefault() ?? dataInstance as DbEntityNameComponent;
            var nameValue = (dataInstance as CompositeResult)?.Values.OfType<DbPhoneticValue>().FirstOrDefault() ?? context.FirstOrDefault<DbPhoneticValue>(o => o.Key == nameComp.ValueKey);
            return new EntityNameComponent()
            {
                ComponentTypeKey = nameComp.ComponentTypeKey,
                PhoneticAlgorithmKey = nameValue.PhoneticAlgorithmKey,
                PhoneticCode = nameValue.PhoneticCode,
                Value = nameValue.Value,
                Key = nameComp.Key,
                SourceEntityKey = nameComp.SourceKey
            };
        }

        /// <summary>
        /// Insert context
        /// </summary>
        public override Core.Model.Entities.EntityNameComponent InsertInternal(DataContext context, Core.Model.Entities.EntityNameComponent data, IPrincipal principal)
        {
            if (data.ComponentType != null) data.ComponentType = data.ComponentType?.EnsureExists(context, principal) as Concept;
            data.ComponentTypeKey = data.ComponentType?.Key ?? data.ComponentTypeKey;
            return base.InsertInternal(context, data, principal);
        }

        /// <summary>
        /// Update
        /// </summary>
        public override Core.Model.Entities.EntityNameComponent UpdateInternal(DataContext context, Core.Model.Entities.EntityNameComponent data, IPrincipal principal)
        {
            if (data.ComponentType != null) data.ComponentType = data.ComponentType?.EnsureExists(context, principal) as Concept;

            data.ComponentTypeKey = data.ComponentType?.Key ?? data.ComponentTypeKey;
            return base.UpdateInternal(context, data, principal);
        }

        /// <summary>
        /// Get components from source
        /// </summary>
        public IEnumerable GetFromSource(DataContext context, Guid id, decimal? versionSequenceId, IPrincipal principal)
        {
            int tr = 0;
            return this.QueryInternal(context, base.BuildSourceQuery<EntityNameComponent>(id), Guid.Empty, 0, null, out tr, principal, false);
        }
    }
}
