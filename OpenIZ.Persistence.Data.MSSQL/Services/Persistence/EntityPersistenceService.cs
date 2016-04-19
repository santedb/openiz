﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MARC.HI.EHRS.SVC.Core.Data;
using System.Linq.Expressions;
using System.Security.Principal;
using OpenIZ.Core.Model.Entities;
using OpenIZ.Persistence.Data.MSSQL.Data;

namespace OpenIZ.Persistence.Data.MSSQL.Services.Persistence
{
    /// <summary>
    /// Entity persistence service
    /// </summary>
    public class EntityPersistenceService : BaseDataPersistenceService<Core.Model.Entities.Entity>
    {
        /// <summary>
        /// Convert entity from model to the data domain
        /// </summary>
        internal override object ConvertFromModel(Core.Model.Entities.Entity model)
        {
            return s_mapper.MapModelInstance<Core.Model.Entities.Entity, Data.EntityVersion>(model);

        }

        /// <summary>
        /// Convert entity model to model class
        /// </summary>
        internal override Core.Model.Entities.Entity ConvertToModel(object data)
        {
            if (data == null)
                return null;

            var entity = data as EntityVersion;

            var retVal = DataCache.Current.Get(entity.EntityVersionId) as Core.Model.Entities.Entity;
            if (retVal == null)
            {
                retVal = this.ConvertItem(entity);

                // Load fast?
                if (retVal != null && entity.Entity != null)
                {
                    ConceptPersistenceService cp = new ConceptPersistenceService();
                    retVal.ClassConcept = cp.ConvertItem(entity.Entity.ClassConcept.CurrentVersion());
                    retVal.StatusConcept = cp.ConvertItem(entity.StatusConcept.CurrentVersion());
                    retVal.DeterminerConcept = cp.ConvertItem(entity.Entity.DeterminerConcept.CurrentVersion());
                    retVal.TypeConcept = cp.ConvertItem(entity.TypeConcept.CurrentVersion());

                    EntityAddressPersistenceService adService = new EntityAddressPersistenceService();

                    // Entity delay load
                    retVal.SetDelayLoadProperties(
                        null,
                        entity.Entity.EntityAddresses.Where(o => entity.VersionSequenceId >= o.EffectiveVersionSequenceId && (o.ObsoleteVersionSequenceId == null || entity.VersionSequenceId < o.ObsoleteVersionSequenceId)).Select(o => adService.ConvertToModel(o)).AsParallel().ToList(),
                        null,
                        null
                        );
                }
            }

            return retVal;



        }

        /// <summary>
        /// Retrieves the specified data from the data store
        /// </summary>
        internal override Core.Model.Entities.Entity Get(Identifier<Guid> containerId, IPrincipal principal, bool loadFast, ModelDataContext dataContext)
        {
            if (containerId == null)
                throw new ArgumentNullException(nameof(containerId));

            EntityVersion tRetVal = null;
            if (containerId.VersionId != default(Guid))
                tRetVal = dataContext.EntityVersions.SingleOrDefault(o => o.EntityVersionId == containerId.VersionId);
            else if (containerId.Id != default(Guid))
                tRetVal = dataContext.EntityVersions.SingleOrDefault(o => o.EntityId == containerId.Id && o.ObsoletionTime == null);

            // Return value
            if (tRetVal == null)
                return null;
            else
                return this.ConvertToModel(tRetVal);

        }

        /// <summary>
        /// Insert the entity into the data store
        /// </summary>
        internal override Core.Model.Entities.Entity Insert(Core.Model.Entities.Entity storageData, IPrincipal principal, ModelDataContext dataContext)
        {
            // Convert to entity
            var domainValue = this.ConvertFromModel(storageData) as Data.EntityVersion;
            if (domainValue.Entity == null)
                domainValue.Entity = new Data.Entity()
                {
                    ClassConceptId = storageData.ClassConcept?.EnsureExists(principal, dataContext).Key ?? storageData.ClassConceptKey,
                    DeterminerConceptId = storageData.DeterminerConcept?.EnsureExists(principal, dataContext).Key ??  storageData.DeterminerConceptKey
                };

            // Insert
            domainValue.CreatedByEntity = principal.GetUser(dataContext);
            domainValue.CreationTime = DateTimeOffset.Now;
            dataContext.EntityVersions.InsertOnSubmit(domainValue);
            dataContext.SubmitChanges();

            storageData.Key = domainValue.EntityId;
            storageData.VersionKey = domainValue.EntityVersionId;
            storageData.VersionSequence = domainValue.VersionSequenceId;

            // Ensure that the addresses / names / etc for the entity exist
            if (storageData.Addresses != null)
            {
                EntityAddressPersistenceService eaps = new EntityAddressPersistenceService();
                foreach (var ad in storageData.Addresses)
                {
                    ad.SourceEntityKey = domainValue.EntityId;
                    eaps.Insert(ad, principal, dataContext, false);
                }
            }

            if (storageData.Names != null)
            {
                EntityNamePersistenceService enps = new EntityNamePersistenceService();
                foreach (var nm in storageData.Names)
                {
                    nm.SourceEntityKey = domainValue.EntityId;
                    enps.Insert(nm, principal, dataContext, false);
                }
            }

            return storageData;
            
        }

        internal override Core.Model.Entities.Entity Obsolete(Core.Model.Entities.Entity storageData, IPrincipal principal, ModelDataContext dataContext)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Query the specified data from the database
        /// </summary>
        internal override IQueryable<Core.Model.Entities.Entity> Query(Expression<Func<Core.Model.Entities.Entity, bool>> query, IPrincipal principal, ModelDataContext dataContext)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            var domainQuery = s_mapper.MapModelExpression<Core.Model.Entities.Entity, Data.EntityVersion>(query);

            return dataContext.EntityVersions.Where(domainQuery)
                .OrderByDescending(o => o.VersionSequenceId)
                .Join(dataContext.Entities, e => e.EntityId, v => v.EntityId, (v,e)=> new { ent = e, ver = v })
                .Join(dataContext.Concepts, c => c.ent.ClassConceptId, r => r.ConceptId, (a, b) => new { ver = a.ver, ent = a.ent, clazz = b })
                .Join(dataContext.Concepts, c => c.ent.DeterminerConceptId, r => r.ConceptId, (a, b) => new { ver = a.ver, ent = a.ent, clazz = a.clazz, determiner = b })
                .Join(dataContext.Concepts, c => c.ver.StatusConceptId, r => r.ConceptId, (a, b) => new { ver = a.ver, ent = a.ent, clazz = a.clazz, determiner = a.determiner, status = b })
                .Select(o => this.ConvertToModel(o.ver, o.ent, o.clazz, o.determiner, o.status));

        }

        /// <summary>
        /// Convert a search result (joined object) 
        /// </summary>
        private Core.Model.Entities.Entity ConvertToModel(EntityVersion ver, Data.Entity ent, Concept clazz, Concept determiner, Concept status)
        {
            var retVal = this.ConvertToModel(ver);

            ConceptPersistenceService cp = new ConceptPersistenceService();
            retVal.ClassConcept = cp.ConvertItem(clazz.CurrentVersion());
            retVal.StatusConcept = cp.ConvertItem(status.CurrentVersion());
            retVal.DeterminerConcept = cp.ConvertItem(determiner.CurrentVersion());

            return retVal;
        }

        internal override Core.Model.Entities.Entity Update(Core.Model.Entities.Entity storageData, IPrincipal principal, ModelDataContext dataContext)
        {
            throw new NotImplementedException();
        }

    }
}
