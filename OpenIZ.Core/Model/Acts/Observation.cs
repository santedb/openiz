﻿using OpenIZ.Core.Model.Attributes;
using OpenIZ.Core.Model.Constants;
using OpenIZ.Core.Model.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Core.Model.Acts
{
    /// <summary>
    /// Represents a class which is an observation
    /// </summary>
    [DataContract(Name = "Observation", Namespace = "http://openiz.org/model")]
    [Serializable]
    public abstract class Observation : Act
    {

        // Interpreation concept key
        private Guid? m_interpretationConceptKey;
        // Interpretation concept
        private Concept m_interpretationConcept;

        /// <summary>
        /// Observation ctor
        /// </summary>
        public Observation()
        {
            this.ClassConceptKey = ActClassKeys.Observation;
        }

        /// <summary>
        /// Gets or sets the interpretation concept
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        [DataMember(Name = "interpretationConcept")]
        public Guid? InterpretationConceptKey
        {
            get { return this.m_interpretationConceptKey; }
            set
            {
                this.m_interpretationConceptKey = value;
                this.m_interpretationConcept = null;
            }
        }

        /// <summary>
        /// Gets or sets the concept which indicates the interpretation of the observtion
        /// </summary>
        [DelayLoad(nameof(InterpretationConceptKey))]
        [IgnoreDataMember]
        public Concept InterpretationConcept
        {
            get {
                this.m_interpretationConcept = base.DelayLoad(this.m_interpretationConceptKey, this.m_interpretationConcept);
                return this.m_interpretationConcept;
            }
            set
            {
                this.m_interpretationConcept = value;
                this.m_interpretationConceptKey = value?.Key;
            }
        }
    }

    /// <summary>
    /// Represents an observation that contains a quantity
    /// </summary>
    [DataContract(Name = "QuantityObservation", Namespace = "http://openiz.org/model")]
    [Serializable]
    public class QuantityObservation : Observation
    {

        // UOM key
        private Guid m_unitOfMeasureKey;
        // UOM
        private Concept m_unitOfMeasure;

        /// <summary>
        /// Gets or sets the observed quantity
        /// </summary>
        [DataMember(Name = "value")]
        public Decimal Value { get; set; }

        /// <summary>
        /// Gets or sets the key of the uom concept
        /// </summary>
        [DataMember(Name = "unitOfMeasure")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        public Guid UnitOfMeasureKey
        {
            get { return this.m_unitOfMeasureKey; }
            set
            {
                this.m_unitOfMeasureKey = value;
                this.m_unitOfMeasure = null;
            }
        }

        /// <summary>
        /// Gets or sets the unit of measure
        /// </summary>
        [IgnoreDataMember]
        [DelayLoad(nameof(UnitOfMeasureKey))]
        public Concept UnitOfMeasure
        {
            get
            {
                this.m_unitOfMeasure = base.DelayLoad(this.m_unitOfMeasureKey, this.m_unitOfMeasure);
                return this.m_unitOfMeasure;
            }
            set
            {
                this.m_unitOfMeasure = value;
                if (value != null)
                    this.m_unitOfMeasureKey = value.Key;
                else
                    this.m_unitOfMeasureKey = Guid.Empty;
            }
        }

    }

    /// <summary>
    /// Represents an observation with a text value
    /// </summary>
    [DataContract(Name = "TextObservation", Namespace = "http://openiz.org/model")]
    [Serializable]
    public class TextObservation : Observation
    {
        /// <summary>
        /// Gets or sets the textual value
        /// </summary>
        [DataMember(Name = "value")]
        public String Value { get; set; }
    }

    /// <summary>
    /// Represents an observation with a concept value
    /// </summary>
    [DataContract(Name = "CodedObservation", Namespace = "http://openiz.org/model")]
    [Serializable]
    public class CodedObservation : Observation
    {

        // Value key
        private Guid m_valueKey;
        // Value
        private Concept m_value;

        /// <summary>
        /// Gets or sets the key of the uom concept
        /// </summary>
        [DataMember(Name = "value")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        public Guid ValueKey
        {
            get { return this.m_valueKey; }
            set
            {
                this.m_valueKey = value;
                this.m_value = null;
            }
        }

        /// <summary>
        /// Gets or sets the coded value of the observation
        /// </summary>
        [IgnoreDataMember]
        [DelayLoad(nameof(ValueKey))]
        public Concept Value
        {
            get
            {
                this.m_value = base.DelayLoad(this.m_valueKey, this.m_value);
                return this.m_value;
            }
            set
            {
                this.m_value = value;
                if (value == null)
                    this.m_valueKey = Guid.Empty;
                else
                    this.m_valueKey = value.Key;
            }
        }
    }

}