﻿/*
 * Copyright 2015-2016 Mohawk College of Applied Arts and Technology
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
 * Date: 2016-8-2
 */
using OpenIZ.Core.Model.Roles;
using OpenIZ.Persistence.Data.ADO.Data;
using OpenIZ.Persistence.Data.ADO.Data.Model;
using OpenIZ.Persistence.Data.ADO.Data.Model.Entities;
using OpenIZ.Persistence.Data.ADO.Data.Model.Roles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Persistence service which is used to persist patients
    /// </summary>
    public class PatientPersistenceService : EntityDerivedPersistenceService<Patient, DbPatient, CompositeResult<Patient, DbPerson, DbEntityVersion, DbEntity>>
    {

        private PersonPersistenceService m_personPersister = new PersonPersistenceService();

        /// <summary>
        /// From model instance
        /// </summary>
        public override object FromModelInstance(Patient modelInstance, DataContext context, IPrincipal principal)
        {
            var dbPatient = base.FromModelInstance(modelInstance, context, principal) as DbPatient;
            
            if (modelInstance.DeceasedDatePrecision.HasValue)
                dbPatient.DeceasedDatePrecision = PersonPersistenceService.PrecisionMap[modelInstance.DeceasedDatePrecision.Value];
            return dbPatient;
        }

        /// <summary>
        /// Model instance
        /// </summary>
        public Core.Model.Roles.Patient ToModelInstance(DbPatient patientInstance, DbPerson personInstance, DbEntityVersion entityVersionInstance, DbEntity entityInstance, DataContext context, IPrincipal principal)
        {
            var retVal = this.m_entityPersister.ToModelInstance<Core.Model.Roles.Patient>(entityVersionInstance, entityInstance, context, principal);

            retVal.DeceasedDate = patientInstance.DeceasedDate;
            // Reverse lookup
            if (!String.IsNullOrEmpty(patientInstance.DeceasedDatePrecision))
                retVal.DeceasedDatePrecision = PersonPersistenceService.PrecisionMap.Where(o => o.Value == patientInstance.DeceasedDatePrecision).Select(o => o.Key).First();

            retVal.MultipleBirthOrder = (int?)patientInstance.MultipleBirthOrder;
            retVal.GenderConceptKey = patientInstance.GenderConceptKey;

            // Copy from person 
            retVal.DateOfBirth = personInstance?.DateOfBirth;

            // Reverse lookup
            if (!String.IsNullOrEmpty(personInstance?.DateOfBirthPrecision))
                retVal.DateOfBirthPrecision = PersonPersistenceService.PrecisionMap.Where(o => o.Value == personInstance.DateOfBirthPrecision).Select(o => o.Key).First();

            retVal.LanguageCommunication = context.Query<DbPersonLanguageCommunication>(v => v.SourceKey == entityInstance.Key && v.EffectiveVersionSequenceId <= entityVersionInstance.VersionSequenceId && (v.ObsoleteVersionSequenceId == null || v.ObsoleteVersionSequenceId >= entityVersionInstance.VersionSequenceId))
                    .Select(o => new Core.Model.Entities.PersonLanguageCommunication(o.LanguageCode, o.IsPreferred)
                    {
                        Key = o.Key
                    })
                    .ToList();

            return retVal;
        }

        /// <summary>
        /// Insert the specified person into the database
        /// </summary>
        public override Core.Model.Roles.Patient Insert(DataContext context, Core.Model.Roles.Patient data, IPrincipal principal)
        {
            data.GenderConcept?.EnsureExists(context, principal);
            data.GenderConceptKey = data.GenderConcept?.Key ?? data.GenderConceptKey;
            this.m_personPersister.Insert(context, data, principal);
            return base.Insert(context, data, principal);
        }

        /// <summary>
        /// Update the specified person
        /// </summary>
        public override Core.Model.Roles.Patient Update(DataContext context, Core.Model.Roles.Patient data, IPrincipal principal)
        {
            // Ensure exists
           data.GenderConcept?.EnsureExists(context, principal);
           data.GenderConceptKey =data.GenderConcept?.Key ??data.GenderConceptKey;

            this.m_personPersister.Update(context, data, principal);
            return base.Update(context, data, principal);
        }

        /// <summary>
        /// Obsolete the object
        /// </summary>
        public override Core.Model.Roles.Patient Obsolete(DataContext context, Core.Model.Roles.Patient data, IPrincipal principal)
        {
            this.m_personPersister.Obsolete(context, data, principal);
        }
        
    }
}
