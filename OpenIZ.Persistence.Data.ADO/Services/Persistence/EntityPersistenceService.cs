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
using OpenIZ.Core.Model.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenIZ.Core.Model.DataTypes;
using OpenIZ.Core.Model.Constants;
using System.Security.Principal;
using OpenIZ.Persistence.Data.ADO.Data.Model;
using OpenIZ.Core.Model.EntityLoader;
using OpenIZ.Core.Model;
using OpenIZ.Persistence.Data.ADO.Data.Model.Entities;
using OpenIZ.Persistence.Data.ADO.Data;
using OpenIZ.Persistence.Data.ADO.Data.Model.Extensibility;
using OpenIZ.Persistence.Data.ADO.Data.Model.Roles;
using OpenIZ.Persistence.Data.ADO.Data.Model.DataType;
using OpenIZ.OrmLite;
using System.Diagnostics;
using OpenIZ.Core.Model.Roles;
using OpenIZ.Core.Services;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Data;
using MARC.HI.EHRS.SVC.Core.Services;
using OpenIZ.Persistence.Data.ADO.Data.Model.Concepts;
using OpenIZ.Persistence.Data.ADO.Data.Model.Security;

namespace OpenIZ.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Entity persistence service
    /// </summary>
    public class EntityPersistenceService : VersionedDataPersistenceService<Core.Model.Entities.Entity, DbEntityVersion, DbEntity>, IReportProgressChanged
    {
        /// <summary>
        /// Progress has changed
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// To model instance
        /// </summary>
        public virtual TEntityType ToModelInstance<TEntityType>(DbEntityVersion dbVersionInstance, DbEntity entInstance, DataContext context, IPrincipal principal) where TEntityType : Core.Model.Entities.Entity, new()
        {
            var retVal = m_mapper.MapDomainInstance<DbEntityVersion, TEntityType>(dbVersionInstance);

            if (retVal == null) return null;

            retVal.ClassConceptKey = entInstance.ClassConceptKey;
            retVal.DeterminerConceptKey = entInstance.DeterminerConceptKey;
            retVal.TemplateKey = entInstance.TemplateKey;
            // Inversion relationships
            //if (retVal.Relationships != null)
            //{
            //    retVal.Relationships.RemoveAll(o => o.InversionIndicator);
            //    retVal.Relationships.AddRange(context.EntityAssociations.Where(o => o.TargetEntityId == retVal.Key.Value).Distinct().Select(o => new EntityRelationship(o.AssociationTypeConceptId, o.TargetEntityId)
            //    {
            //        SourceEntityKey = o.SourceEntityId,
            //        Key = o.EntityAssociationId,
            //        InversionIndicator = true
            //    }));
            //}
            return retVal;
        }

        /// <summary>
        /// Convert to model instance
        /// </summary>
        public override Core.Model.Entities.Entity ToModelInstance(object dataInstance, DataContext context, IPrincipal principal)
        {

#if DEBUG
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif 
            if (dataInstance == null)
                return null;
            // Alright first, which type am I mapping to?
            var dbEntityVersion = (dataInstance as CompositeResult)?.Values.OfType<DbEntityVersion>().FirstOrDefault() ?? dataInstance as DbEntityVersion ?? context.FirstOrDefault<DbEntityVersion>(o => o.VersionKey == (dataInstance as DbEntitySubTable).ParentKey);
            var dbEntity = (dataInstance as CompositeResult)?.Values.OfType<DbEntity>().FirstOrDefault() ?? context.FirstOrDefault<DbEntity>(o => o.Key == dbEntityVersion.Key);
            Entity retVal = null;

            if (dbEntityVersion.StatusConceptKey == StatusKeys.Purged) // Purged data doesn't exist
                retVal = this.ToModelInstance<Entity>(dbEntityVersion, dbEntity, context, principal);
            else switch (dbEntity.ClassConceptKey.ToString().ToLower())
            {
                case EntityClassKeyStrings.Device:
                    retVal = new DeviceEntityPersistenceService().ToModelInstance(
                        (dataInstance as CompositeResult)?.Values.OfType<DbDeviceEntity>().FirstOrDefault() ?? context.FirstOrDefault<DbDeviceEntity>(o => o.ParentKey == dbEntityVersion.VersionKey),
                        dbEntityVersion,
                        dbEntity,
                        context,
                        principal);
                    break;
                case EntityClassKeyStrings.NonLivingSubject:
                    retVal = new ApplicationEntityPersistenceService().ToModelInstance(
                        (dataInstance as CompositeResult)?.Values.OfType<DbApplicationEntity>().FirstOrDefault() ?? context.FirstOrDefault<DbApplicationEntity>(o => o.ParentKey == dbEntityVersion.VersionKey),
                        dbEntityVersion,
                        dbEntity,
                        context,
                        principal);
                    break;
                case EntityClassKeyStrings.Person:
                    var ue = (dataInstance as CompositeResult)?.Values.OfType<DbUserEntity>().FirstOrDefault() ?? context.FirstOrDefault<DbUserEntity>(o => o.ParentKey == dbEntityVersion.VersionKey);

                    if (ue != null)
                        retVal = new UserEntityPersistenceService().ToModelInstance(
                            ue,
                            (dataInstance as CompositeResult)?.Values.OfType<DbPerson>().FirstOrDefault() ?? context.FirstOrDefault<DbPerson>(o => o.ParentKey == dbEntityVersion.VersionKey),
                            dbEntityVersion,
                            dbEntity,
                            context,
                            principal);
                    else
                        retVal = new PersonPersistenceService().ToModelInstance(
                            (dataInstance as CompositeResult)?.Values.OfType<DbPerson>().FirstOrDefault() ?? context.FirstOrDefault<DbPerson>(o => o.ParentKey == dbEntityVersion.VersionKey),
                            dbEntityVersion,
                            dbEntity,
                            context,
                            principal);
                    break;
                case EntityClassKeyStrings.Patient:
                    retVal = new PatientPersistenceService().ToModelInstance(
                        (dataInstance as CompositeResult)?.Values.OfType<DbPatient>().FirstOrDefault() ?? context.FirstOrDefault<DbPatient>(o => o.ParentKey == dbEntityVersion.VersionKey),
                        (dataInstance as CompositeResult)?.Values.OfType<DbPerson>().FirstOrDefault() ?? context.FirstOrDefault<DbPerson>(o => o.ParentKey == dbEntityVersion.VersionKey),
                        dbEntityVersion,
                        dbEntity,
                        context,
                        principal);
                    break;
                case EntityClassKeyStrings.Provider:
                    retVal = new ProviderPersistenceService().ToModelInstance(
                        (dataInstance as CompositeResult)?.Values.OfType<DbProvider>().FirstOrDefault() ?? context.FirstOrDefault<DbProvider>(o => o.ParentKey == dbEntityVersion.VersionKey),
                        (dataInstance as CompositeResult)?.Values.OfType<DbPerson>().FirstOrDefault() ?? context.FirstOrDefault<DbPerson>(o => o.ParentKey == dbEntityVersion.VersionKey),
                        dbEntityVersion,
                        dbEntity,
                        context,
                        principal);
                    break;
                case EntityClassKeyStrings.Place:
                case EntityClassKeyStrings.CityOrTown:
                case EntityClassKeyStrings.Country:
                case EntityClassKeyStrings.CountyOrParish:
                case EntityClassKeyStrings.State:
                case EntityClassKeyStrings.ServiceDeliveryLocation:
                    retVal = new PlacePersistenceService().ToModelInstance(
                        (dataInstance as CompositeResult)?.Values.OfType<DbPlace>().FirstOrDefault() ?? context.FirstOrDefault<DbPlace>(o => o.ParentKey == dbEntityVersion.VersionKey),
                        dbEntityVersion,
                        dbEntity,
                        context,
                        principal);
                    break;
                case EntityClassKeyStrings.Organization:
                    retVal = new OrganizationPersistenceService().ToModelInstance(
                        (dataInstance as CompositeResult)?.Values.OfType<DbOrganization>().FirstOrDefault() ?? context.FirstOrDefault<DbOrganization>(o => o.ParentKey == dbEntityVersion.VersionKey),
                        dbEntityVersion,
                        dbEntity,
                        context,
                        principal);
                    break;
                case EntityClassKeyStrings.Material:
                    retVal = new MaterialPersistenceService().ToModelInstance<Material>(
                        (dataInstance as CompositeResult)?.Values.OfType<DbMaterial>().FirstOrDefault() ?? context.FirstOrDefault<DbMaterial>(o => o.ParentKey == dbEntityVersion.VersionKey),
                        dbEntityVersion,
                        dbEntity,
                        context,
                        principal);
                    break;
                case EntityClassKeyStrings.ManufacturedMaterial:
                    retVal = new ManufacturedMaterialPersistenceService().ToModelInstance(
                        (dataInstance as CompositeResult)?.Values.OfType<DbManufacturedMaterial>().FirstOrDefault() ?? context.FirstOrDefault<DbManufacturedMaterial>(o => o.ParentKey == dbEntityVersion.VersionKey),
                        (dataInstance as CompositeResult)?.Values.OfType<DbMaterial>().FirstOrDefault() ?? context.FirstOrDefault<DbMaterial>(o => o.ParentKey == dbEntityVersion.VersionKey),
                        dbEntityVersion,
                        dbEntity,
                        context,
                        principal);
                    break;
                default:
                    retVal = this.ToModelInstance<Entity>(dbEntityVersion, dbEntity, context, principal);
                    break;

            }

#if DEBUG
            sw.Stop();
            this.m_tracer.TraceEvent(TraceEventType.Verbose, 0, "Conversion of {0} took: {1} ms", retVal, sw.ElapsedMilliseconds);
#endif 
            retVal.LoadAssociations(context, principal);
            return retVal;
        }

        /// <summary>
        /// Conversion based on type
        /// </summary>
        protected override Entity CacheConvert(object dataInstance, DataContext context, IPrincipal principal)
        {
            return this.DoCacheConvert(dataInstance, context, principal);
        }

        /// <summary>
        /// Perform the cache convert
        /// </summary>
        internal Entity DoCacheConvert(object dataInstance, DataContext context, IPrincipal principal) { 
            if (dataInstance == null)
                return null;
            // Alright first, which type am I mapping to?
            var dbEntityVersion = (dataInstance as CompositeResult)?.Values.OfType<DbEntityVersion>().FirstOrDefault() ?? dataInstance as DbEntityVersion ?? context.FirstOrDefault<DbEntityVersion>(o => o.VersionKey == (dataInstance as DbEntitySubTable).ParentKey);
            var dbEntity = (dataInstance as CompositeResult)?.Values.OfType<DbEntity>().FirstOrDefault() ?? context.FirstOrDefault<DbEntity>(o => o.Key == dbEntityVersion.Key);
            Entity retVal = null;
            var cache = new AdoPersistenceCache(context);

            if (!dbEntityVersion.ObsoletionTime.HasValue)
                switch (dbEntity.ClassConceptKey.ToString().ToUpper())
                {
                    case EntityClassKeyStrings.Device:
                        retVal = cache?.GetCacheItem<DeviceEntity>(dbEntity.Key);
                        break;
                    case EntityClassKeyStrings.NonLivingSubject:
                        retVal = cache?.GetCacheItem<ApplicationEntity>(dbEntity.Key);
                        break;
                    case EntityClassKeyStrings.Person:
                        var ue = (dataInstance as CompositeResult)?.Values.OfType<DbUserEntity>().FirstOrDefault() ?? context.FirstOrDefault<DbUserEntity>(o => o.ParentKey == dbEntityVersion.VersionKey);
                        if (ue != null)
                            retVal = cache?.GetCacheItem<UserEntity>(dbEntity.Key);

                        else
                            retVal = cache?.GetCacheItem<Person>(dbEntity.Key);
                        break;
                    case EntityClassKeyStrings.Patient:
                        retVal = cache?.GetCacheItem<Patient>(dbEntity.Key);
                        break;
                    case EntityClassKeyStrings.Provider:
                        retVal = cache?.GetCacheItem<Provider>(dbEntity.Key);

                        break;
                    case EntityClassKeyStrings.Place:
                    case EntityClassKeyStrings.CityOrTown:
                    case EntityClassKeyStrings.Country:
                    case EntityClassKeyStrings.CountyOrParish:
                    case EntityClassKeyStrings.State:
                    case EntityClassKeyStrings.ServiceDeliveryLocation:
                        retVal = cache?.GetCacheItem<Place>(dbEntity.Key);

                        break;
                    case EntityClassKeyStrings.Organization:
                        retVal = cache?.GetCacheItem<Organization>(dbEntity.Key);

                        break;
                    case EntityClassKeyStrings.Material:
                        retVal = cache?.GetCacheItem<Material>(dbEntity.Key);

                        break;
                    case EntityClassKeyStrings.ManufacturedMaterial:
                        retVal = cache?.GetCacheItem<ManufacturedMaterial>(dbEntity.Key);

                        break;
                    default:
                        retVal = cache?.GetCacheItem<Entity>(dbEntity.Key);
                        break;
                }

            // Return cache value
            if (retVal != null)
                return retVal;
            else
                return base.CacheConvert(dataInstance, context, principal);
        }

        /// <summary>
        /// Insert the specified entity into the data context
        /// </summary>
        public Core.Model.Entities.Entity InsertCoreProperties(DataContext context, Core.Model.Entities.Entity data, IPrincipal principal)
        {

            // Ensure FK exists
            if (data.ClassConcept != null) data.ClassConcept = data.ClassConcept?.EnsureExists(context, principal) as Concept;
            if (data.DeterminerConcept != null) data.DeterminerConcept = data.DeterminerConcept?.EnsureExists(context, principal) as Concept;
            if (data.StatusConcept != null) data.StatusConcept = data.StatusConcept?.EnsureExists(context, principal) as Concept;
            if (data.TypeConcept != null) data.TypeConcept = data.TypeConcept?.EnsureExists(context, principal) as Concept;
            if (data.Template != null) data.Template = data.Template?.EnsureExists(context, principal) as TemplateDefinition;
            data.TypeConceptKey = data.TypeConcept?.Key ?? data.TypeConceptKey;
            data.DeterminerConceptKey = data.DeterminerConcept?.Key ?? data.DeterminerConceptKey;
            data.ClassConceptKey = data.ClassConcept?.Key ?? data.ClassConceptKey;
            data.StatusConceptKey = data.StatusConcept?.Key ?? data.StatusConceptKey;
            data.StatusConceptKey = data.StatusConceptKey == Guid.Empty || data.StatusConceptKey == null ? StatusKeys.New : data.StatusConceptKey;

            var retVal = base.InsertInternal(context, data, principal);

            // Identifiers
	        if (data.Identifiers != null)
	        {
		        // Validate unique values for IDs
		        var uniqueIds = data.Identifiers.Where(o => o.AuthorityKey.HasValue).Where(o => ApplicationContext.Current.GetService<IDataPersistenceService<AssigningAuthority>>().Get(new Identifier<Guid>(o.AuthorityKey.Value), principal, true)?.IsUnique == true);

		        foreach (var entityIdentifier in uniqueIds)
		        {
			        if (context.Query<DbEntityIdentifier>(c => c.SourceKey != data.Key && c.AuthorityKey == entityIdentifier.AuthorityKey && c.Value == entityIdentifier.Value && c.ObsoleteVersionSequenceId == null).Any())
			        {
						throw new DuplicateNameException(entityIdentifier.Value);
					}
				}

				base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.EntityIdentifier, DbEntityIdentifier>(
					data.Identifiers.Where(o => o != null && !o.IsEmpty()),
					retVal,
					context,
					principal);
			}

            // Relationships
            if (data.Relationships != null) 
                base.UpdateVersionedAssociatedItems<Core.Model.Entities.EntityRelationship, DbEntityRelationship>(
                   data.Relationships.Where(o => o != null && !o.InversionIndicator && !o.IsEmpty()).ToList(),
                    retVal,
                    context,
                    principal);

            // Telecoms
            if (data.Telecoms != null)
                base.UpdateVersionedAssociatedItems<Core.Model.Entities.EntityTelecomAddress, DbTelecomAddress>(
                   data.Telecoms.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context,
                    principal);

            // Extensions
            if (data.Extensions != null)
                base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.EntityExtension, DbEntityExtension>(
                   data.Extensions.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context,
                    principal);

            // Names
            if (data.Names != null)
                base.UpdateVersionedAssociatedItems<Core.Model.Entities.EntityName, DbEntityName>(
                   data.Names.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context,
                    principal);

            // Addresses
            if (data.Addresses != null)
                base.UpdateVersionedAssociatedItems<Core.Model.Entities.EntityAddress, DbEntityAddress>(
                   data.Addresses.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context,
                    principal);

            // Notes
            if (data.Notes != null)
                base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.EntityNote, DbEntityNote>(
                   data.Notes.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context,
                    principal);

            // Tags
            if (data.Tags != null)
                base.UpdateAssociatedItems<Core.Model.DataTypes.EntityTag, DbEntityTag>(
                   data.Tags.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context,
                    principal);


            return retVal;
        }

        /// <summary>
        /// Update the specified entity
        /// </summary>
        public Core.Model.Entities.Entity UpdateCoreProperties(DataContext context, Core.Model.Entities.Entity data, IPrincipal principal)
        {
            // Esnure exists
            if (data.ClassConcept != null) data.ClassConcept = data.ClassConcept?.EnsureExists(context, principal) as Concept;
            if (data.DeterminerConcept != null) data.DeterminerConcept = data.DeterminerConcept?.EnsureExists(context, principal) as Concept;
            if (data.StatusConcept != null) data.StatusConcept = data.StatusConcept?.EnsureExists(context, principal) as Concept;
            if (data.Template != null) data.Template = data.Template?.EnsureExists(context, principal) as TemplateDefinition;
            if (data.TypeConcept != null) data.TypeConcept = data.TypeConcept?.EnsureExists(context, principal) as Concept;
            data.TypeConceptKey = data.TypeConcept?.Key ?? data.TypeConceptKey;
            data.DeterminerConceptKey = data.DeterminerConcept?.Key ?? data.DeterminerConceptKey;
            data.ClassConceptKey = data.ClassConcept?.Key ?? data.ClassConceptKey;
            data.StatusConceptKey = data.StatusConcept?.Key ?? data.StatusConceptKey;
            data.StatusConceptKey = data.StatusConceptKey == Guid.Empty || data.StatusConceptKey == null ? StatusKeys.New : data.StatusConceptKey;

            var retVal = base.UpdateInternal(context, data, principal);


            // Identifiers
	        if (data.Identifiers != null)
	        {
				// Validate unique values for IDs
		        var uniqueIds = data.Identifiers.Where(o => o.AuthorityKey.HasValue).Where(o => ApplicationContext.Current.GetService<IDataPersistenceService<AssigningAuthority>>().Get(new Identifier<Guid>(o.AuthorityKey.Value), principal, true)?.IsUnique == true);

		        foreach (var entityIdentifier in uniqueIds)
		        {
			        if (context.Query<DbEntityIdentifier>(c => c.SourceKey != data.Key && c.AuthorityKey == entityIdentifier.AuthorityKey && c.Value == entityIdentifier.Value &&  c.ObsoleteVersionSequenceId == null).Any())
			        {
				        throw new DuplicateNameException(entityIdentifier.Value);
			        }
		        }

		        base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.EntityIdentifier, DbEntityIdentifier>(
			        data.Identifiers.Where(o => !o.IsEmpty()),
			        retVal,
			        context,
			        principal);
			}

            // Relationships
            if (data.Relationships != null)
                base.UpdateVersionedAssociatedItems<Core.Model.Entities.EntityRelationship, DbEntityRelationship>(
                   data.Relationships.Where(o => o != null && !o.InversionIndicator && !o.IsEmpty() && (o.SourceEntityKey == data.Key || !o.SourceEntityKey.HasValue)).ToList(),
                    retVal,
                    context,
                    principal);

            // Telecoms
            if (data.Telecoms != null)
                base.UpdateVersionedAssociatedItems<Core.Model.Entities.EntityTelecomAddress, DbTelecomAddress>(
                   data.Telecoms.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context,
                    principal);

            // Extensions
            if (data.Extensions != null)
                base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.EntityExtension, DbEntityExtension>(
                   data.Extensions.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context,
                    principal);

            // Names
            if (data.Names != null)
                base.UpdateVersionedAssociatedItems<Core.Model.Entities.EntityName, DbEntityName>(
                   data.Names.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context,
                    principal);

            // Addresses
            if (data.Addresses != null)
                base.UpdateVersionedAssociatedItems<Core.Model.Entities.EntityAddress, DbEntityAddress>(
                   data.Addresses.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context,
                    principal);

            // Notes
            if (data.Notes != null)
                base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.EntityNote, DbEntityNote>(
                   data.Notes.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context,
                    principal);

            // Tags
            if (data.Tags != null)
                base.UpdateAssociatedItems<Core.Model.DataTypes.EntityTag, DbEntityTag>(
                   data.Tags.Where(o => o != null && !o.IsEmpty()),
                    retVal,
                    context,
                    principal);


            return retVal;
        }

        /// <summary>
        /// Obsoleted status key
        /// </summary>
        public override Core.Model.Entities.Entity ObsoleteInternal(DataContext context, Core.Model.Entities.Entity data, IPrincipal principal)
        {
            data.StatusConceptKey = StatusKeys.Obsolete;
            return base.UpdateInternal(context, data, principal);
        }

        /// <summary>
        /// Insert the entity
        /// </summary>
        public override Entity InsertInternal(DataContext context, Entity data, IPrincipal principal)
        {
            switch (data.ClassConceptKey.ToString().ToUpper())
            {
                case EntityClassKeyStrings.Device:
                    return new DeviceEntityPersistenceService().InsertInternal(context, data.Convert<DeviceEntity>(), principal);
                case EntityClassKeyStrings.NonLivingSubject:
                    return new ApplicationEntityPersistenceService().InsertInternal(context, data.Convert<ApplicationEntity>(), principal);
                case EntityClassKeyStrings.Person:
                    return new PersonPersistenceService().InsertInternal(context, data.Convert<Person>(), principal);
                case EntityClassKeyStrings.Patient:
                    return new PatientPersistenceService().InsertInternal(context, data.Convert<Patient>(), principal);
                case EntityClassKeyStrings.Provider:
                    return new ProviderPersistenceService().InsertInternal(context, data.Convert<Provider>(), principal);
                case EntityClassKeyStrings.Place:
                case EntityClassKeyStrings.CityOrTown:
                case EntityClassKeyStrings.Country:
                case EntityClassKeyStrings.CountyOrParish:
                case EntityClassKeyStrings.State:
                case EntityClassKeyStrings.ServiceDeliveryLocation:
                    return new PlacePersistenceService().InsertInternal(context, data.Convert<Place>(), principal);
                case EntityClassKeyStrings.Organization:
                    return new OrganizationPersistenceService().InsertInternal(context, data.Convert<Organization>(), principal);
                case EntityClassKeyStrings.Material:
                    return new MaterialPersistenceService().InsertInternal(context, data.Convert<Material>(), principal);
                case EntityClassKeyStrings.ManufacturedMaterial:
                    return new ManufacturedMaterialPersistenceService().InsertInternal(context, data.Convert<ManufacturedMaterial>(), principal);
                default:
                    return this.InsertCoreProperties(context, data, principal);

            }
        }

        /// <summary>
        /// Update entity
        /// </summary>
        public override Entity UpdateInternal(DataContext context, Entity data, IPrincipal principal)
        {
            switch (data.ClassConceptKey.ToString().ToUpper())
            {
                case EntityClassKeyStrings.Device:
                    return new DeviceEntityPersistenceService().UpdateInternal(context, data.Convert<DeviceEntity>(), principal);
                case EntityClassKeyStrings.NonLivingSubject:
                    return new ApplicationEntityPersistenceService().UpdateInternal(context, data.Convert<ApplicationEntity>(), principal);
                case EntityClassKeyStrings.Person:
                    return new PersonPersistenceService().UpdateInternal(context, data.Convert<Person>(), principal);
                case EntityClassKeyStrings.Patient:
                    return new PatientPersistenceService().UpdateInternal(context, data.Convert<Patient>(), principal);
                case EntityClassKeyStrings.Provider:
                    return new ProviderPersistenceService().UpdateInternal(context, data.Convert<Provider>(), principal);
                case EntityClassKeyStrings.Place:
                case EntityClassKeyStrings.CityOrTown:
                case EntityClassKeyStrings.Country:
                case EntityClassKeyStrings.CountyOrParish:
                case EntityClassKeyStrings.State:
                case EntityClassKeyStrings.ServiceDeliveryLocation:
                    return new PlacePersistenceService().UpdateInternal(context, data.Convert<Place>(), principal);
                case EntityClassKeyStrings.Organization:
                    return new OrganizationPersistenceService().UpdateInternal(context, data.Convert<Organization>(), principal);
                case EntityClassKeyStrings.Material:
                    return new MaterialPersistenceService().UpdateInternal(context, data.Convert<Material>(), principal);
                case EntityClassKeyStrings.ManufacturedMaterial:
                    return new ManufacturedMaterialPersistenceService().UpdateInternal(context, data.Convert<ManufacturedMaterial>(), principal);
                default:
                    return this.UpdateCoreProperties(context, data, principal);

            }
        }

        /// <summary>
        /// Perform a purge of this data
        /// </summary>
        protected override void BulkPurgeInternal(DataContext context, IPrincipal principal, Guid[] keysToPurge)
        {
            // Purge the related fields
            int ofs = 0;
            while (ofs < keysToPurge.Length)
            {
                var batchKeys = keysToPurge.Skip(ofs).Take(100).ToArray();
                ofs += 100;

                this.ProgressChanged?.Invoke(this, new ProgressChangedEventArgs((float)ofs / (float)keysToPurge.Length, "Purging Entities")); 

                // Purge the related fields
                var versionKeys = context.Query<DbEntityVersion>(o => batchKeys.Contains(o.Key)).Select(o => o.VersionKey).ToArray();

                // Delete versions of this entity in sub table
                context.Delete<DbPatient>(o => versionKeys.Contains(o.ParentKey));
                context.Delete<DbProvider>(o => versionKeys.Contains(o.ParentKey));
                context.Delete<DbUserEntity>(o => versionKeys.Contains(o.ParentKey));

                // Person fields
                context.Delete<DbPersonLanguageCommunication>(o => batchKeys.Contains(o.SourceKey));
                context.Delete<DbPerson>(o => versionKeys.Contains(o.ParentKey));

                // Other entities
                context.Delete<DbDeviceEntity>(o => versionKeys.Contains(o.ParentKey));
                context.Delete<DbApplicationEntity>(o => versionKeys.Contains(o.ParentKey));
                context.Delete<DbPlace>(o => versionKeys.Contains(o.ParentKey));
                context.Delete<DbManufacturedMaterial>(o => versionKeys.Contains(o.ParentKey));
                context.Delete<DbMaterial>(o => versionKeys.Contains(o.ParentKey));
                // Purge the related entity fields

                // Names
                var col = TableMapping.Get(typeof(DbEntityNameComponent)).Columns.First(o => o.IsPrimaryKey);
                var deleteStatement = context.CreateSqlStatement<DbEntityNameComponent>()
                    .SelectFrom(col)
                    .InnerJoin<DbEntityNameComponent, DbEntityName>(o => o.SourceKey, o => o.Key)
                    .Where<DbEntityName>(o => batchKeys.Contains(o.SourceKey));
                context.Delete<DbEntityNameComponent>(deleteStatement);
                context.Delete<DbEntityName>(o => batchKeys.Contains(o.SourceKey));

                // Addresses
                col = TableMapping.Get(typeof(DbEntityAddressComponent)).Columns.First(o => o.IsPrimaryKey);
                deleteStatement = context.CreateSqlStatement<DbEntityAddressComponent>()
                    .SelectFrom(col)
                    .InnerJoin<DbEntityAddressComponent, DbEntityAddress>(o => o.SourceKey, o => o.Key)
                    .Where<DbEntityAddress>(o => batchKeys.Contains(o.SourceKey));
                context.Delete<DbEntityAddressComponent>(deleteStatement);
                context.Delete<DbEntityAddress>(o => batchKeys.Contains(o.SourceKey));

                // Other Relationships
                context.Delete<DbEntityRelationship>(o => batchKeys.Contains(o.SourceKey));
                context.Delete<DbEntityIdentifier>(o => batchKeys.Contains(o.SourceKey));
                context.Delete<DbEntityExtension>(o => batchKeys.Contains(o.SourceKey));
                context.Delete<DbEntityTag>(o => batchKeys.Contains(o.SourceKey));
                context.Delete<DbEntityNote>(o => batchKeys.Contains(o.SourceKey));

                // Detach keys which are being deleted will need to be removed from the version heirarchy
                foreach (var rpl in context.Query<DbEntityVersion>(o => versionKeys.Contains(o.ReplacesVersionKey.Value)).ToArray())
                {
                    rpl.ReplacesVersionKey = null;
                    rpl.ReplacesVersionKeySpecified = true;
                    context.Update(rpl);
                }

                // Purge the core entity data
                context.Delete<DbEntityVersion>(o => batchKeys.Contains(o.Key));

                // Create a version which indicates this is PURGED
                context.Insert(
                    context.Query<DbEntity>(o => batchKeys.Contains(o.Key))
                    .Select(o => o.Key)
                    .Distinct()
                    .ToArray()
                    .Select(o => new DbEntityVersion()
                    {
                        CreatedByKey = principal.GetUserKey(context).GetValueOrDefault(),
                        CreationTime = DateTimeOffset.Now,
                        Key = o,
                        StatusConceptKey = StatusKeys.Purged
                    }));

            }

            context.ResetSequence("ENT_VRSN_SEQ", context.Query<DbEntityVersion>(o => true).Max(o => o.VersionSequenceId));

        }

        /// <summary>
        /// Copy entity data from <paramref name="fromContext"/> to <paramref name="toContext"/>
        /// </summary>
        public override void Copy(Guid[] keysToCopy, DataContext fromContext, DataContext toContext)
        {
            toContext.InsertOrUpdate(fromContext.Query<DbPhoneticValue>(o => o.SequenceId >= 0));
            toContext.InsertOrUpdate(fromContext.Query<DbEntityAddressComponentValue>(o => o.SequenceId >= 0));

            // Copy over users and protocols and other act tables
            IEnumerable<Guid> additionalKeys = fromContext.Query<DbExtensionType>(o => o.ObsoletionTime == null)
                .Select(o => o.CreatedByKey)
                .Distinct()
                .Union(
                    fromContext.Query<DbAssigningAuthority>(o => o.ObsoletionTime == null)
                    .Select(o => o.CreatedByKey)
                    .Distinct()
                )
                .Union(
                    fromContext.Query<DbTemplateDefinition>(o => o.ObsoletionTime == null)
                    .Select(o => o.CreatedByKey)
                    .Distinct()
                )
                .Union(
                    fromContext.Query<DbSecurityDevice>(o => o.ObsoletionTime == null)
                    .Select(o => o.CreatedByKey)
                    .Distinct()
                )
                .ToArray();
            toContext.InsertOrUpdate(fromContext.Query<DbSecurityUser>(o => additionalKeys.Contains(o.Key)));
            toContext.InsertOrUpdate(fromContext.Query<DbExtensionType>(o => o.ObsoletionTime == null));
            toContext.InsertOrUpdate(fromContext.Query<DbTemplateDefinition>(o => o.ObsoletionTime == null));

            additionalKeys = fromContext.Query<DbAssigningAuthority>(o => o.ObsoletionTime == null)
                .Select(o => o.AssigningDeviceKey).Distinct().ToArray()
                .Where(o => o.HasValue)
                .Select(o => o.Value);

            toContext.InsertOrUpdate(fromContext.Query<DbSecurityDevice>(o => additionalKeys.Contains(o.Key)).ToArray().Select(o => new DbSecurityDevice()
            {
                Key = o.Key,
                CreatedByKey = o.CreatedByKey,
                CreationTime = o.CreationTime,
                PublicId = o.PublicId,
                ObsoletionTime = o.ObsoletionTime,
                ObsoletedByKey = o.ObsoletedByKey,
                DeviceSecret = o.DeviceSecret ?? "XXXX",
            }));
            toContext.InsertOrUpdate(fromContext.Query<DbAssigningAuthority>(o => o.ObsoletionTime == null));

            additionalKeys = fromContext.Query<DbEntityRelationship>(o => o.ObsoleteVersionSequenceId == null)
                   .Select(o => o.RelationshipTypeKey)
                   .Distinct()
                   .Union(
                        fromContext.Query<DbEntity>(o => o.Key != null)
                        .Select(o => o.DeterminerConceptKey)
                        .Distinct()
                    ).Union(
                        fromContext.Query<DbEntity>(o => o.Key != null)
                        .Select(o => o.ClassConceptKey)
                        .Distinct()
                    ).Union(
                        fromContext.Query<DbEntityVersion>(o => o.VersionSequenceId > 0)
                        .Select(o => o.StatusConceptKey)
                        .Distinct()
                    ).Union(
                        fromContext.Query<DbEntityVersion>(o => o.VersionSequenceId > 0)
                        .Select(o => o.TypeConceptKey)
                        .Distinct()
                        .ToArray()
                        .Where(o => o.HasValue)
                        .Select(o => o.Value)
                    ).Union(
                        fromContext.Query<DbPatient>(o => o.ParentKey != null)
                        .Select(o => o.GenderConceptKey)
                        .Distinct()
                    )
                   .ToArray();
 
            toContext.InsertOrUpdate(fromContext.Query<DbConceptClass>(o => true));
            toContext.InsertOrUpdate(fromContext.Query<DbConcept>(o => additionalKeys.Contains(o.Key)));
            toContext.InsertOrUpdate(fromContext.Query<DbConceptVersion>(o => additionalKeys.Contains(o.Key)).OrderBy(o => o.VersionSequenceId));
            toContext.InsertOrUpdate(fromContext.Query<DbConceptSet>(o => true));
            toContext.InsertOrUpdate(fromContext.Query<DbConceptSetConceptAssociation>(o => additionalKeys.Contains(o.ConceptKey)));

            // Purge the related fields
            int ofs = 0;
            while (ofs < keysToCopy.Length)
            {
                var batchKeys = keysToCopy.Skip(ofs).Take(100).ToArray();
                ofs += 100;

                this.ProgressChanged?.Invoke(this, new ProgressChangedEventArgs((float)ofs / (float)keysToCopy.Length, "Copying Entities"));

                var versionKeys = fromContext.Query<DbEntityVersion>(o => batchKeys.Contains(o.Key)).Select(o => o.VersionKey).ToArray();

                // copy the core entity data
                toContext.InsertOrUpdate(fromContext.Query<DbEntity>(o => batchKeys.Contains(o.Key)));

                // copy the core entity data
                toContext.InsertOrUpdate(fromContext.Query<DbConcept>(o => additionalKeys.Contains(o.Key)));

                // Copy users of interest
                additionalKeys = fromContext.Query<DbEntityVersion>(o => batchKeys.Contains(o.Key))
                    .Select(o => o.CreatedByKey)
                    .Distinct()
                    .Union(
                        fromContext.Query<DbEntityVersion>(o => batchKeys.Contains(o.Key))
                        .Select(o => o.ObsoletedByKey)
                        .Distinct()
                        .ToArray()
                        .Where(o => o.HasValue)
                        .Select(o => o.Value)
                    )
                    .ToArray();
                toContext.InsertOrUpdate(fromContext.Query<DbSecurityUser>(o => additionalKeys.Contains(o.Key)));

                toContext.InsertOrUpdate(fromContext.Query<DbEntityVersion>(o => batchKeys.Contains(o.Key)).OrderBy(o=>o.VersionSequenceId));

                additionalKeys = fromContext.Query<DbPlaceService>(o => batchKeys.Contains(o.SourceKey))
                    .Select(o => o.ServiceConceptKey)
                    .Distinct()
                    .ToArray();
                toContext.InsertOrUpdate(fromContext.Query<DbConcept>(o => additionalKeys.Contains(o.Key)));
                toContext.InsertOrUpdate(fromContext.Query<DbPlaceService>(o => batchKeys.Contains(o.SourceKey)));

                // Person fields
                toContext.InsertOrUpdate(fromContext.Query<DbPerson>(o => versionKeys.Contains(o.ParentKey)));
                toContext.InsertOrUpdate(fromContext.Query<DbPersonLanguageCommunication>(o => batchKeys.Contains(o.SourceKey)));

                // Copy versions of persons
                toContext.InsertOrUpdate(fromContext.Query<DbPatient>(o => versionKeys.Contains(o.ParentKey)));

                additionalKeys = fromContext.Query<DbProvider>(o => versionKeys.Contains(o.ParentKey))
                    .Select(o => o.Specialty)
                    .Distinct()
                    .ToArray();
                toContext.InsertOrUpdate(fromContext.Query<DbConcept>(o => additionalKeys.Contains(o.Key)));

                toContext.InsertOrUpdate(fromContext.Query<DbProvider>(o => versionKeys.Contains(o.ParentKey)));

                // Security Users
                additionalKeys = fromContext.Query<DbUserEntity>(o => versionKeys.Contains(o.ParentKey))
                    .Select(o => o.SecurityUserKey)
                    .Distinct()
                    .ToArray();
                toContext.InsertOrUpdate(fromContext.Query<DbSecurityUser>(o => additionalKeys.Contains(o.Key)));
                toContext.InsertOrUpdate(fromContext.Query<DbUserEntity>(o => versionKeys.Contains(o.ParentKey)));

                // Other entities
                additionalKeys = fromContext.Query<DbDeviceEntity>(o => versionKeys.Contains(o.ParentKey))
                    .Select(o => o.SecurityDeviceKey)
                    .Distinct()
                    .ToArray();
                toContext.InsertOrUpdate(fromContext.Query<DbSecurityDevice>(o => additionalKeys.Contains(o.Key)));
                toContext.InsertOrUpdate(fromContext.Query<DbDeviceEntity>(o => versionKeys.Contains(o.ParentKey)));

                additionalKeys = fromContext.Query<DbApplicationEntity>(o => versionKeys.Contains(o.ParentKey))
                   .Select(o => o.SecurityApplicationKey)
                   .Distinct()
                   .ToArray();
                toContext.InsertOrUpdate(fromContext.Query<DbSecurityApplication>(o => additionalKeys.Contains(o.Key)));
                toContext.InsertOrUpdate(fromContext.Query<DbApplicationEntity>(o => versionKeys.Contains(o.ParentKey)));
                toContext.InsertOrUpdate(fromContext.Query<DbPlace>(o => versionKeys.Contains(o.ParentKey)));

                // Person fields
                additionalKeys = fromContext.Query<DbMaterial>(o => versionKeys.Contains(o.ParentKey))
                    .Select(o => o.QuantityConceptKey)
                    .Distinct()
                    .Union(
                        fromContext.Query<DbMaterial>(o => versionKeys.Contains(o.ParentKey))
                        .Select(o => o.FormConceptKey)
                        .Distinct()
                    )
                    .ToArray();
                toContext.InsertOrUpdate(fromContext.Query<DbConcept>(o => additionalKeys.Contains(o.Key)));
                toContext.InsertOrUpdate(fromContext.Query<DbMaterial>(o => versionKeys.Contains(o.ParentKey)));
                toContext.InsertOrUpdate(fromContext.Query<DbManufacturedMaterial>(o => versionKeys.Contains(o.ParentKey)));

                // Names
                toContext.InsertOrUpdate(fromContext.Query<DbEntityName>(o => batchKeys.Contains(o.SourceKey)));

                // Insert component links
                var selectStatement = fromContext.CreateSqlStatement<DbEntityNameComponent>()
                    .SelectFrom()
                    .InnerJoin<DbEntityNameComponent, DbEntityName>(o => o.SourceKey, o => o.Key)
                    .Where<DbEntityName>(o => batchKeys.Contains(o.SourceKey));
                toContext.InsertOrUpdate(fromContext.Query<DbEntityNameComponent>(selectStatement));

                // Addresses

                toContext.InsertOrUpdate(fromContext.Query<DbEntityAddress>(o => batchKeys.Contains(o.SourceKey)));

                selectStatement = fromContext.CreateSqlStatement<DbEntityAddressComponent>()
                    .SelectFrom()
                    .InnerJoin<DbEntityAddressComponent, DbEntityAddress>(o => o.SourceKey, o => o.Key)
                    .Where<DbEntityAddress>(o => batchKeys.Contains(o.SourceKey));
                toContext.InsertOrUpdate(fromContext.Query<DbEntityAddressComponent>(selectStatement));

                // Other Relationships
                toContext.InsertOrUpdate(fromContext.Query<DbEntityIdentifier>(o => batchKeys.Contains(o.SourceKey)));

                // Entity Extension types
                toContext.InsertOrUpdate(fromContext.Query<DbEntityExtension>(o => batchKeys.Contains(o.SourceKey)));
                toContext.InsertOrUpdate(fromContext.Query<DbEntityTag>(o => batchKeys.Contains(o.SourceKey)));
                toContext.InsertOrUpdate(fromContext.Query<DbEntityNote>(o => batchKeys.Contains(o.SourceKey)));

                additionalKeys = fromContext.Query<DbEntityRelationship>(o => batchKeys.Contains(o.SourceKey))
                    .Select(o => o.TargetKey)
                    .Distinct()
                    .ToArray();
                toContext.InsertOrUpdate(fromContext.Query<DbEntity>(o => additionalKeys.Contains(o.Key)));


                toContext.InsertOrUpdate(fromContext.Query<DbEntityRelationship>(o => batchKeys.Contains(o.SourceKey)));
            }

            toContext.ResetSequence("ENT_VRSN_SEQ", toContext.Query<DbEntityVersion>(o => true).Max(o => o.VersionSequenceId));

        }


    }
}
