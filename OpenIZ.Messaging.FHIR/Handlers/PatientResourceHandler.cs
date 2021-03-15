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
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Backbone;
using MARC.HI.EHRS.SVC.Messaging.FHIR.DataTypes;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Resources;
using OpenIZ.Core.Model;
using OpenIZ.Core.Model.Constants;
using OpenIZ.Core.Model.DataTypes;
using OpenIZ.Core.Model.Entities;
using OpenIZ.Core.Services;
using OpenIZ.Messaging.FHIR.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Web;
using DatePrecision = OpenIZ.Core.Model.DataTypes.DatePrecision;

namespace OpenIZ.Messaging.FHIR.Handlers
{
	/// <summary>
	/// Represents a resource handler which can handle patients.
	/// </summary>
	public class PatientResourceHandler : RepositoryResourceHandlerBase<Patient, Core.Model.Roles.Patient>
	{
		/// <summary>
		/// The repository.
		/// </summary>
		private IPatientRepositoryService repository;

		// IDs of family members
		private List<Guid> m_familyMembers;
		private List<Guid> m_contacts;
		/// <summary>
		/// Resource handler subscription
		/// </summary>
		public PatientResourceHandler()
		{
			ApplicationContext.Current.Started += (o, e) =>
			{
				this.repository = ApplicationContext.Current.GetService<IPatientRepositoryService>();
				this.m_familyMembers = ApplicationContext.Current.GetService<IConceptRepositoryService>().FindConcepts(x => x.ConceptSets.Any(c => c.Mnemonic == "FamilyMember")).Select(c => c.Key.Value).ToList();
				this.m_contacts = new List<Guid>();
				this.m_contacts.Add(EntityRelationshipTypeKeys.Employee);
				this.m_contacts.Add(EntityRelationshipTypeKeys.EmergencyContact);
				this.m_contacts.Add(EntityRelationshipTypeKeys.CoverageSponsor);
				this.m_contacts.Add(EntityRelationshipTypeKeys.NextOfKin);
			};
		}

		/// <summary>
		/// Map a patient object to FHIR.
		/// </summary>
		/// <param name="model">The model.</param>
		/// <returns>Returns the mapped FHIR resource.</returns>
		protected override Patient MapToFhir(Core.Model.Roles.Patient model, WebOperationContext webOperationContext)
		{
			var retVal = DataTypeConverter.CreateResource<Patient>(model);
			retVal.Active = model.StatusConceptKey == StatusKeys.Active;
			retVal.Address = model.LoadCollection<EntityAddress>("Addresses").Select(o => DataTypeConverter.ToFhirAddress(o)).ToList();
			retVal.BirthDate = model.DateOfBirth;
            retVal.BirthDate.Precision = model.DateOfBirthPrecision == DatePrecision.Day ? MARC.HI.EHRS.SVC.Messaging.FHIR.DataTypes.DatePrecision.Day : MARC.HI.EHRS.SVC.Messaging.FHIR.DataTypes.DatePrecision.Month;
			retVal.Deceased = model.DeceasedDate == DateTime.MinValue ? (object)new FhirBoolean(true) : model.DeceasedDate != null ? new FhirDate(model.DeceasedDate.Value) : null;
			retVal.Gender = DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>("GenderConcept"))?.GetPrimaryCode()?.Code;
			retVal.Identifier = model.Identifiers?.Select(o => DataTypeConverter.ToFhirIdentifier(o)).ToList();
			retVal.MultipleBirth = model.MultipleBirthOrder == 0 ? (FhirElement)new FhirBoolean(true) : model.MultipleBirthOrder.HasValue ? new FhirInt(model.MultipleBirthOrder.Value) : null;
			retVal.Name = model.LoadCollection<EntityName>("Names").Select(o => DataTypeConverter.ToFhirHumanName(o)).ToList();
			retVal.Timestamp = model.ModifiedOn.DateTime;
			retVal.Telecom = model.LoadCollection<EntityTelecomAddress>("Telecoms").Select(o => DataTypeConverter.ToFhirTelecom(o)).ToList();

			// TODO: Relationships
			foreach (var rel in model.LoadCollection<EntityRelationship>("Relationships").Where(o => !o.InversionIndicator))
			{
				// Contact => Person
				if (this.m_familyMembers.Contains(rel.RelationshipTypeKey.Value) ||
					rel.RelationshipTypeKey == EntityRelationshipTypeKeys.Contact)
				{
					// Create the relative object
					var relative = DataTypeConverter.CreateResource<RelatedPerson>(rel.LoadProperty<Core.Model.Entities.Person>(nameof(EntityRelationship.TargetEntity)));
					relative.Relationship = DataTypeConverter.ToFhirCodeableConcept(rel.LoadProperty<Concept>(nameof(EntityRelationship.RelationshipType)));

					var relatedPerson = rel.LoadProperty<Person>(nameof(EntityRelationship.TargetEntity));
					relative.Address = DataTypeConverter.ToFhirAddress(relatedPerson.LoadCollection<EntityAddress>(nameof(Entity.Addresses)).FirstOrDefault());
					// TODO: Refactor this (see DSM-42 issue ticket)
					relative.DateOfBirth = relatedPerson.DateOfBirth;

					//relative.Gender = DataTypeConverter.ToFhirCodeableConcept((rel.TargetEntity as Core.Model.Roles.Patient)?.LoadProperty<Concept>(nameof(Core.Model.Roles.Patient.GenderConcept)));
					relative.Identifier = relatedPerson.LoadCollection<EntityIdentifier>(nameof(Entity.Identifiers)).Select(o => DataTypeConverter.ToFhirIdentifier(o)).ToList();
					relative.Name = relatedPerson.LoadCollection<EntityName>(nameof(Entity.Names)).Select(o => DataTypeConverter.ToFhirHumanName(o)).FirstOrDefault();
					relative.Patient = DataTypeConverter.CreateInternalReference<Patient>(model);
					relative.Telecom = rel.TargetEntity.LoadCollection<EntityTelecomAddress>(nameof(Entity.Telecoms)).Select(o => DataTypeConverter.ToFhirTelecom(o)).ToList();
					retVal.Contained.Add(new ContainedResource()
					{
						Item = relative
					});

					relative.Extension.AddRange(DataTypeConverter.GetCommonExtendedAttributes(relatedPerson, webOperationContext));
				}
				else if (this.m_contacts.Contains(rel.RelationshipTypeKey.Value))
				{
					var person = rel.LoadProperty<Entity>(nameof(EntityRelationship.TargetEntity));


					var contact = new PatientContact()
					{
						XmlId = $"urn:uuid:{person.Key}",
						
						Address = DataTypeConverter.ToFhirAddress(person.LoadCollection<EntityAddress>(nameof(Entity.Addresses)).FirstOrDefault()),
						Relationship = new List<FhirCodeableConcept>() { DataTypeConverter.ToFhirCodeableConcept(rel.LoadProperty<Concept>(nameof(EntityRelationship.RelationshipType)), "http://terminology.hl7.org/CodeSystem/v2-0131") },
						Name = DataTypeConverter.ToFhirHumanName(person.LoadCollection<EntityName>(nameof(Entity.Names)).FirstOrDefault()),
						// TODO: Gender
						Telecom = person.LoadCollection<EntityTelecomAddress>(nameof(Entity.Telecoms)).Select(t => DataTypeConverter.ToFhirTelecom(t)).ToList(),
					};

					DataTypeConverter.AddExtensions(person, contact);
					contact.Extension.AddRange(DataTypeConverter.GetCommonExtendedAttributes(person, webOperationContext));


					retVal.Contact.Add(contact);

					// TODO: 
					//retVal.Link.Add(new Patient.LinkComponent()
					//{
					//    Other = DataTypeConverter.CreateNonVersionedReference<RelatedPerson>(rel.TargetEntityKey, restOperationContext),
					//    Type = Patient.LinkType.Seealso
					//});
				}
				else if (rel.RelationshipTypeKey == EntityRelationshipTypeKeys.Replaces)
					retVal.Link.Add(new PatientLink()
					{
						Type = PatientLinkType.Replace,
						Other = DataTypeConverter.CreateReference<Patient>(rel.LoadProperty<Entity>(nameof(EntityRelationship.TargetEntity)), webOperationContext)
					});
				
			}

			var photo = model.LoadCollection<EntityExtension>("Extensions").FirstOrDefault(o => o.ExtensionTypeKey == ExtensionTypeKeys.JpegPhotoExtension);
			if (photo != null)
				retVal.Photo = new List<Attachment>() {
					new Attachment()
					{
						ContentType = "image/jpg",
						Data = photo.ExtensionValueXml
					}
				};

			// TODO: Links
			retVal.Extension.AddRange(DataTypeConverter.GetCommonExtendedAttributes(model, webOperationContext));
			return retVal;
		}

		/// <summary>
		/// Maps a FHIR patient resource to an IMSI patient.
		/// </summary>
		/// <param name="resource">The resource.</param>
		/// <returns>Returns the mapped model.</returns>
		protected override Core.Model.Roles.Patient MapToModel(Patient resource, WebOperationContext webOperationContext)
		{
			var patient = new Core.Model.Roles.Patient
			{
				Addresses = resource.Address.Select(DataTypeConverter.ToEntityAddress).ToList(),
				CreationTime = DateTimeOffset.Now,
				DateOfBirth = resource.BirthDate?.DateValue,
				// TODO: Extensions
				Extensions = resource.Extension.Select(DataTypeConverter.ToEntityExtension).ToList(),
				GenderConceptKey = DataTypeConverter.ToConcept(new FhirCoding(new Uri("http://hl7.org/fhir/administrative-gender"), resource.Gender?.Value))?.Key,
				Identifiers = resource.Identifier.Select(DataTypeConverter.ToEntityIdentifier).ToList(),
				LanguageCommunication = resource.Communication.Select(DataTypeConverter.ToPersonLanguageCommunication).ToList(),
				Key = Guid.NewGuid(),
				Names = resource.Name.Select(DataTypeConverter.ToEntityName).ToList(),
				Relationships = resource.Contact.Select(DataTypeConverter.ToEntityRelationship).ToList(),
				StatusConceptKey = resource.Active?.Value == true ? StatusKeys.Active : StatusKeys.Obsolete,
				Telecoms = resource.Telecom.Select(DataTypeConverter.ToEntityTelecomAddress).ToList()
			};

			Guid key;

			if (!Guid.TryParse(resource.Id, out key))
			{
				key = Guid.NewGuid();
			}

			patient.Key = key;

			if (resource.Deceased is FhirDateTime)
			{
				patient.DeceasedDate = (FhirDateTime)resource.Deceased;
			}
			else if (resource.Deceased is FhirBoolean)
			{
				// we don't have a field for "deceased indicator" to say that the patient is dead, but we don't know that actual date/time of death
				// should find a better way to do this
				patient.DeceasedDate = DateTime.Now;
				patient.DeceasedDatePrecision = DatePrecision.Year;
			}

			if (resource.MultipleBirth is FhirBoolean)
			{
				patient.MultipleBirthOrder = 0;
			}
			else if (resource.MultipleBirth is FhirInt)
			{
				patient.MultipleBirthOrder = ((FhirInt)resource.MultipleBirth).Value;
			}

			return patient;
		}

        /// <summary>
        /// Get interactions
        /// </summary>
        protected override IEnumerable<InteractionDefinition> GetInteractions()
        {
            return new TypeRestfulInteraction[]
            {
                TypeRestfulInteraction.InstanceHistory,
                TypeRestfulInteraction.Read,
                TypeRestfulInteraction.Search,
                TypeRestfulInteraction.VersionRead,
                TypeRestfulInteraction.Delete,
                TypeRestfulInteraction.Create,
                TypeRestfulInteraction.Update
            }.Select(o => new InteractionDefinition() { Type = o });
        }
    }
}