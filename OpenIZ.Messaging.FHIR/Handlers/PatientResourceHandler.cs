﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.SVC.Core.Data;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Resources;
using OpenIZ.Core.Model.Roles;
using OpenIZ.Core.Services;
using MARC.HI.EHRS.SVC.Core;
using OpenIZ.Core.Model.Constants;
using MARC.HI.EHRS.SVC.Messaging.FHIR.DataTypes;
using OpenIZ.Messaging.FHIR.Util;
using System.ServiceModel.Web;

namespace OpenIZ.Messaging.FHIR.Handlers
{
    /// <summary>
    /// Represents a resource handler which can handle patients 
    /// </summary>
    public class PatientResourceHandler : ResourceHandlerBase<MARC.HI.EHRS.SVC.Messaging.FHIR.Resources.Patient, Core.Model.Roles.Patient>
    {

        // Repository 
        private IPatientRepositoryService m_repository;

        /// <summary>
        /// Resource handler subscription
        /// </summary>
        public PatientResourceHandler()
        {
            ApplicationContext.Current.Started += (o, e) => this.m_repository = ApplicationContext.Current.GetService<IPatientRepositoryService>();
        }

        /// <summary>
        /// Create the specified patient instance
        /// </summary>
        protected override Core.Model.Roles.Patient Create(Core.Model.Roles.Patient modelInstance, List<IResultDetail> issues, TransactionMode mode)
        {
            return this.m_repository.Insert(modelInstance);
        }

        /// <summary>
        /// Delete the specified patient
        /// </summary>
        protected override Core.Model.Roles.Patient Delete(Guid modelId, List<IResultDetail> details)
        {
            return this.m_repository.Obsolete(modelId);
        }

        /// <summary>
        /// Map a patient object to FHIR
        /// </summary>
        protected override MARC.HI.EHRS.SVC.Messaging.FHIR.Resources.Patient MapToFhir(Core.Model.Roles.Patient model)
        {
            var retVal = DatatypeConverter.CreateResource<MARC.HI.EHRS.SVC.Messaging.FHIR.Resources.Patient>(model);
            retVal.Active = model.StatusConceptKey == StatusKeys.Active;
            retVal.Address = model.Addresses.Select(o => DatatypeConverter.Convert(o)).ToList();
            retVal.BirthDate = model.DateOfBirth;
            retVal.Deceased = model.DeceasedDate == DateTime.MinValue ? (object)new FhirBoolean(true) : model.DeceasedDate != null ? new FhirDate(model.DeceasedDate.Value) : null;
            retVal.Gender = DatatypeConverter.Convert(model.GenderConcept)?.GetPrimaryCode()?.Code;
            retVal.Identifier = model.Identifiers?.Select(o => DatatypeConverter.Convert(o)).ToList();
            retVal.MultipleBirth = model.MultipleBirthOrder == 0 ? (FhirElement)new FhirBoolean(true) : model.MultipleBirthOrder.HasValue ? new FhirInt(model.MultipleBirthOrder.Value) : null;
            retVal.Name = model.Names.Select(o => DatatypeConverter.Convert(o)).ToList();
            retVal.Timestamp = model.ModifiedOn.DateTime;
            retVal.Telecom = model.Telecoms.Select(o => DatatypeConverter.Convert(o)).ToList();

            // TODO: Relationships
            foreach (var rel in model.Relationships.Where(o=>!o.InversionIndicator))
            {
                // Family member
                if (rel.RelationshipType.ConceptSets.Any(o => o.Key == ConceptSetKeys.FamilyMember))
                {
                    // Create the relative object
                    var relative = DatatypeConverter.CreateResource<RelatedPerson>(rel.TargetEntity);
                    relative.Relationship = DatatypeConverter.Convert(rel.RelationshipType);
                    relative.Address = DatatypeConverter.Convert(rel.TargetEntity.Addresses.FirstOrDefault());
                    relative.Gender = DatatypeConverter.Convert((rel.TargetEntity as Core.Model.Roles.Patient)?.GenderConcept);
                    relative.Identifier = rel.TargetEntity.Identifiers.Select(o => DatatypeConverter.Convert(o)).ToList();
                    relative.Name = DatatypeConverter.Convert(rel.TargetEntity.Names.FirstOrDefault());
                    if (rel.TargetEntity is Core.Model.Roles.Patient)
                        relative.Patient = DatatypeConverter.CreateReference<MARC.HI.EHRS.SVC.Messaging.FHIR.Resources.Patient>(rel.TargetEntity);
                    relative.Telecom = rel.TargetEntity.Telecoms.Select(o => DatatypeConverter.Convert(o)).ToList();
                    retVal.Contained.Add(new ContainedResource()
                    {
                        Item = relative
                    });
                }
                else if(rel.RelationshipTypeKey == EntityRelationshipTypeKeys.HealthcareProvider)
                    retVal.Provider = DatatypeConverter.CreateReference<MARC.HI.EHRS.SVC.Messaging.FHIR.Resources.Practictioner>(rel.TargetEntity);
            }

            // TODO: Links
            return retVal;
        }

        protected override Core.Model.Roles.Patient MapToModel(MARC.HI.EHRS.SVC.Messaging.FHIR.Resources.Patient resource)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Query for patients
        /// </summary>
        protected override IEnumerable<Core.Model.Roles.Patient> Query(Expression<Func<Core.Model.Roles.Patient, bool>> query, List<IResultDetail> issues, int offset, int count, out int totalResults)
        {
            Expression<Func<Core.Model.Roles.Patient, bool>> filter = o => o.ClassConceptKey == EntityClassKeys.Patient && o.ObsoletionTime == null ;
            var parm = Expression.Parameter(typeof(Core.Model.Roles.Patient));
            query = Expression.Lambda<Func<Core.Model.Roles.Patient, bool>>(Expression.AndAlso(Expression.Invoke(filter, parm), Expression.Invoke(query, parm)), parm);
            return this.m_repository.Find(query, offset, count, out totalResults);
        }

        /// <summary>
        /// Retrieves the specified patient
        /// </summary>
        protected override Core.Model.Roles.Patient Read(Identifier<Guid> id, List<IResultDetail> details)
        {
            return this.m_repository.Get(id.Id, id.VersionId);
        }

        /// <summary>
        /// Update the specified resource
        /// </summary>
        protected override Core.Model.Roles.Patient Update(Core.Model.Roles.Patient model, List<IResultDetail> details, TransactionMode mode)
        {
            return this.m_repository.Save(model);
        }
    }
}
