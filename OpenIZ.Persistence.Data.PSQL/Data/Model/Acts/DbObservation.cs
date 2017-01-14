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
 * Date: 2016-7-24
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Persistence.Data.PSQL.Data.Model.Acts
{
    /// <summary>
    /// Stores data related to an observation act
    /// </summary>
    [TableName("observation")]
    public class DbObservation : IDbVersionedAssociation
    {

        /// <summary>
        /// Gets or sets the interpretation concept
        /// </summary>
        [Column("interpretationConcept")]
        public Guid InterpretationConceptKey { get; set; }

        /// <summary>
        /// Identifies the value type
        /// </summary>
        [Column("valueType"), MaxLength(2)]
        public String ValueType { get; set; }

    }

    /// <summary>
    /// Represents additional data related to a quantified observation
    /// </summary>
    [TableName("quantity_observation")]
    public class DbQuantityObservation : IDbVersionedAssociation
    {

        /// <summary>
        /// Represents the unit of measure
        /// </summary>
        [Column("unitOfMeasure")]
        public Guid UnitOfMeasureKey { get; set; }

        /// <summary>
        /// Gets or sets the value of the measure
        /// </summary>
        [Column("value")]
        public Decimal Value { get; set; }

    }

    /// <summary>
    /// Identifies the observation as a text obseration
    /// </summary>
    [TableName("text_observation")]
    public class DbTextObservation : IDbVersionedAssociation
    {
        /// <summary>
        /// Gets the value of the observation as a string
        /// </summary>
        [Column("value")]
        public String Value { get; set; }

    }

    /// <summary>
    /// Identifies data related to a coded observation
    /// </summary>
    [TableName("coded_observation")]
    public class DbCodedObservation : IDbVersionedAssociation
    {

        /// <summary>
        /// Gets or sets the concept representing the value of this
        /// </summary>
        [Column("value")] 
        public Guid Value { get; set; }

    }
}
