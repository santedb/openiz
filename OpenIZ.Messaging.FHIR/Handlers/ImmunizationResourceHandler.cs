﻿/*
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
using MARC.Everest.Connectors;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Backbone;
using MARC.HI.EHRS.SVC.Messaging.FHIR.DataTypes;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Resources;
using OpenIZ.Core.Diagnostics;
using OpenIZ.Core.Model;
using OpenIZ.Core.Model.Acts;
using OpenIZ.Core.Model.Constants;
using OpenIZ.Core.Model.DataTypes;
using OpenIZ.Core.Model.Entities;
using OpenIZ.Core.Services;
using OpenIZ.Messaging.FHIR.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.ServiceModel.Web;

namespace OpenIZ.Messaging.FHIR.Handlers
{
    /// <summary>
    /// Resource handler for immunization classes.
    /// </summary>
    public class ImmunizationResourceHandler : RepositoryResourceHandlerBase<Immunization, SubstanceAdministration>, IBundleResourceHandler
    {
        /// <summary>
        /// Maps the substance administration to FHIR.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>Returns the mapped FHIR resource.</returns>
        protected override Immunization MapToFhir(SubstanceAdministration model, WebOperationContext webOperationContext)
        {
            var retVal = DataTypeConverter.CreateResource<Immunization>(model);

            retVal.DoseQuantity = new FhirQuantity()
            {
                Units = DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>("DoseUnit"), "http://hl7.org/fhir/sid/ucum").GetPrimaryCode()?.Code?.Value,
                Value = new FhirDecimal(model.DoseQuantity)
            };
            retVal.Date = (FhirDate)model.ActTime.DateTime;
            retVal.Route = DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>(nameof(SubstanceAdministration.Route)));
            retVal.Site = DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>(nameof(SubstanceAdministration.Site)));
            retVal.Status = "completed";
            //retVal.SelfReported = model.Tags.Any(o => o.TagKey == "selfReported" && Convert.ToBoolean(o.Value));
            retVal.WasNotGiven = model.IsNegated;

            // Material
            var matPtcpt = model.Participations.FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.Consumable) ??
                model.Participations.FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.Product);
            if (matPtcpt != null)
            {
                var matl = matPtcpt.LoadProperty<Material>(nameof(ActParticipation.PlayerEntity));
                retVal.VaccineCode = DataTypeConverter.ToFhirCodeableConcept(matl.LoadProperty<Concept>(nameof(Act.TypeConcept)));
                retVal.ExpirationDate = matl.ExpiryDate.HasValue ? (FhirDate)matl.ExpiryDate : null;
                retVal.LotNumber = (matl as ManufacturedMaterial)?.LotNumber;
            }
            else
                retVal.ExpirationDate = null;

            // RCT
            var rct = model.Participations.FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.RecordTarget);
            if (rct != null)
            {
                retVal.Patient = DataTypeConverter.CreateReference<Patient>(rct.LoadProperty<Entity>("PlayerEntity"), webOperationContext);
            }

            // Performer
            var prf = model.Participations.FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.Performer);
            if (prf != null)
                retVal.Performer = DataTypeConverter.CreateReference<Practitioner>(rct.LoadProperty<Entity>("PlayerEntity"), webOperationContext);

            // Protocol
            foreach (var itm in model.Protocols)
            {
                ImmunizationProtocol protocol = new ImmunizationProtocol();
                var dbProtocol = itm.LoadProperty<Protocol>(nameof(ActProtocol.Protocol));
                protocol.DoseSequence = new FhirInt((int)model.SequenceId);

                // Protocol lookup
                protocol.Series = dbProtocol?.Name;
                retVal.VaccinationProtocol.Add(protocol);
            }
            if (retVal.VaccinationProtocol.Count == 0)
                retVal.VaccinationProtocol.Add(new ImmunizationProtocol() { DoseSequence = (int)model.SequenceId });

            var loc = model.Participations.FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.Location);
            if (loc != null)
                retVal.Extension.Add(new Extension()
                {
                    Url = "http://openiz.org/extensions/act/fhir/location",
                    Value = new FhirString(loc.PlayerEntityKey.ToString())
                });

            return retVal;
        }

        /// <summary>
        /// Map an immunization FHIR resource to a substance administration.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <returns>Returns the mapped model.</returns>
        protected override SubstanceAdministration MapToModel(Immunization resource, WebOperationContext webOperationContext)
        {
            var substanceAdministration = new SubstanceAdministration
            {
                ActTime = resource.Date.DateValue.Value,
                DoseQuantity = resource.DoseQuantity?.Value.Value ?? 0,
                DoseUnit = resource.DoseQuantity != null ? DataTypeConverter.ToConcept<String>(resource.DoseQuantity.Units.Value, "http://hl7.org/fhir/sid/ucum") : null,
                Extensions = resource.Extension?.Select(DataTypeConverter.ToActExtension).ToList(),
                Identifiers = resource.Identifier?.Select(DataTypeConverter.ToActIdentifier).ToList(),
                Key = Guid.NewGuid(),
                MoodConceptKey = ActMoodKeys.Eventoccurrence,
                StatusConceptKey = resource.Status == "completed" ? StatusKeys.Completed : StatusKeys.Nullified,
                RouteKey = DataTypeConverter.ToConcept(resource.Route)?.Key,
                SiteKey = DataTypeConverter.ToConcept(resource.Site)?.Key,
            };

            
            Guid key;
            if (Guid.TryParse(resource.Id, out key))
            {
                substanceAdministration.Key = key;
            }

            // Was not given
            if (resource.WasNotGiven?.Value == true)
            {
                substanceAdministration.IsNegated = true;
            }

            // Patient
            if(resource.Patient != null)
            {
                // Is the subject a uuid
                if (resource.Patient.ReferenceUrl.Value.StartsWith("urn:uuid:"))
                    substanceAdministration.Participations.Add(new ActParticipation(ActParticipationKey.RecordTarget, Guid.Parse(resource.Patient.ReferenceUrl.Value.Substring(9))));
                else throw new NotSupportedException("Only UUID references are supported");
            }

            // Encounter
            if (resource.Encounter != null)
            {
                // Is the subject a uuid
                if (resource.Encounter.ReferenceUrl.Value.StartsWith("urn:uuid:"))
                    substanceAdministration.Relationships.Add(new ActRelationship(ActRelationshipTypeKeys.HasComponent, substanceAdministration.Key)
                    {
                        SourceEntityKey = Guid.Parse(resource.Encounter.ReferenceUrl.Value.Substring(9))
                    });
                else throw new NotSupportedException("Only UUID references are supported");
            }

            // Find the material that was issued
            if (resource.VaccineCode != null)
            {
                var concept = DataTypeConverter.ToConcept(resource.VaccineCode);
                if (concept == null)
                {
                    this.m_traceSource.TraceWarning("Ignoring administration {0} don't have concept mapped", resource.VaccineCode);
                    return null;
                }
                // Get the material 
                int t = 0;
                var material = ApplicationContext.Current.GetService<IMaterialRepositoryService>().FindMaterial(m => m.TypeConceptKey == concept.Key, 0, 1, out t).FirstOrDefault();
                if (material == null)
                {
                    this.m_traceSource.TraceWarning("Ignoring administration {0} don't have material registered for {1}", resource.VaccineCode, concept?.Mnemonic);
                    return null;
                }
                else
                {
                    substanceAdministration.Participations.Add(new ActParticipation(ActParticipationKey.Product, material.Key));
                    if (resource.LotNumber != null)
                    {
                        // TODO: Need to also find where the GTIN is kept
                        var mmaterial = ApplicationContext.Current.GetService<IMaterialRepositoryService>().FindManufacturedMaterial(o => o.LotNumber == resource.LotNumber && o.Relationships.Any(r => r.SourceEntityKey == material.Key && r.RelationshipTypeKey == EntityRelationshipTypeKeys.Instance));
                        substanceAdministration.Participations.Add(new ActParticipation(ActParticipationKey.Consumable, material.Key) { Quantity = 1 });
                    }

                    // Get dose units
                    if (substanceAdministration.DoseQuantity == 0)
                    {
                        substanceAdministration.DoseQuantity = 1;
                        substanceAdministration.DoseUnitKey = material.QuantityConceptKey;
                    }
                }
            }

            return substanceAdministration;
        }

        /// <summary>
        /// Query for substance administrations.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="issues">The issues.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <param name="totalResults">The total results.</param>
        /// <returns>Returns the list of models which match the given parameters.</returns>
        protected override IEnumerable<SubstanceAdministration> Query(Expression<Func<SubstanceAdministration, bool>> query, List<IResultDetail> issues, Guid queryId, int offset, int count, out int totalResults)
        {
            Guid initialImmunization = Guid.Parse("f3be6b88-bc8f-4263-a779-86f21ea10a47"),
                immunization = Guid.Parse("6e7a3521-2967-4c0a-80ec-6c5c197b2178"),
                boosterImmunization = Guid.Parse("0331e13f-f471-4fbd-92dc-66e0a46239d5");

            var obsoletionReference = Expression.MakeBinary(ExpressionType.Equal, Expression.Convert(Expression.MakeMemberAccess(query.Parameters[0], typeof(SubstanceAdministration).GetProperty(nameof(SubstanceAdministration.StatusConceptKey))), typeof(Guid)), Expression.Constant(StatusKeys.Completed));
            var typeReference = Expression.MakeBinary(ExpressionType.Or,
                Expression.MakeBinary(ExpressionType.Or,
                    Expression.MakeBinary(ExpressionType.Equal, Expression.Convert(Expression.MakeMemberAccess(query.Parameters[0], typeof(SubstanceAdministration).GetProperty(nameof(SubstanceAdministration.TypeConceptKey))), typeof(Guid)), Expression.Constant(initialImmunization)),
                    Expression.MakeBinary(ExpressionType.Equal, Expression.Convert(Expression.MakeMemberAccess(query.Parameters[0], typeof(SubstanceAdministration).GetProperty(nameof(SubstanceAdministration.TypeConceptKey))), typeof(Guid)), Expression.Constant(immunization))
                ),
                Expression.MakeBinary(ExpressionType.Equal, Expression.Convert(Expression.MakeMemberAccess(query.Parameters[0], typeof(SubstanceAdministration).GetProperty(nameof(SubstanceAdministration.TypeConceptKey))), typeof(Guid)), Expression.Constant(boosterImmunization))
            );

            query = Expression.Lambda<Func<SubstanceAdministration, bool>>(Expression.AndAlso(Expression.AndAlso(obsoletionReference, query.Body), typeReference), query.Parameters);

            if (queryId == Guid.Empty)
                return this.m_repository.Find(query, offset, count, out totalResults);
            else
                return (this.m_repository as IPersistableQueryRepositoryService).Find<SubstanceAdministration>(query, offset, count, out totalResults, queryId);
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
                TypeRestfulInteraction.Create,
                TypeRestfulInteraction.Update,
                TypeRestfulInteraction.Delete
            }.Select(o => new InteractionDefinition() { Type = o });
        }

        /// <summary>
        /// Map to model
        /// </summary>
        public IdentifiedData MapToModel(BundleEntry bundleResource, WebOperationContext context, Bundle bundle)
        {
            return this.MapToModel(bundleResource.Resource.Resource as Immunization, context);
        }
    }
}