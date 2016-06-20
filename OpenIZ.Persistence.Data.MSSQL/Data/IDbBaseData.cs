﻿using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Persistence.Data.MSSQL.Data
{
    /// <summary>
    /// Represents base data
    /// </summary>
    public interface IDbBaseData : IDbIdentified
    {
        /// <summary>
        /// Gets or sets the entity id which created this
        /// </summary>
        Guid CreatedBy { get; set; }
        /// <summary>
        /// Gets or sets the id which obsoleted this
        /// </summary>
        Guid? ObsoletedBy { get; set; }
        /// <summary>
        /// Gets or sets the creation time
        /// </summary>
        DateTimeOffset CreationTime { get; set; }
        /// <summary>
        /// Gets or sets the obsoletion time
        /// </summary>
        DateTimeOffset? ObsoletionTime { get; set; }
    }

    /// <summary>
    /// Interface definition
    /// </summary>
    public partial class ReferenceTermDisplayName : IDbBaseData
    {
        /// <summary>
        /// Identification for object
        /// </summary>
        [LinqPropertyMap(nameof(ReferenceTermDisplayNameId))]
        public Guid Id
        {
            get
            {
                return this.ReferenceTermDisplayNameId ;
            }
            set
            {
                this.ReferenceTermDisplayNameId = value;
            }
        }

    }

    /// <summary>
    /// Interface definition
    /// </summary>
    public partial class ExtensionType : IDbBaseData
    {
        /// <summary>
        /// Identification for object
        /// </summary>
        [LinqPropertyMap(nameof(ExtensionTypeId))]
        public Guid Id
        {
            get
            {
                return this.ExtensionTypeId;
            }
            set
            {
                this.ExtensionTypeId = value;
            }
        }

    }

    /// <summary>
    /// Interface definition
    /// </summary>
    public partial class ReferenceTerm : IDbBaseData
    {
        /// <summary>
        /// Identification for object
        /// </summary>
        [LinqPropertyMap(nameof(ReferenceTermId))]
        public Guid Id
        {
            get
            {
                return this.ReferenceTermId;
            }
            set
            {
                this.ReferenceTermId = value;
            }
        }

    }

    /// <summary>
    /// Interface definition
    /// </summary>
    public partial class PhoneticAlgorithm : IDbBaseData
    {
        /// <summary>
        /// Identification for object
        /// </summary>
        [LinqPropertyMap(nameof(PhoneticAlgorithmId))]
        public Guid Id
        {
            get
            {
                return this.PhoneticAlgorithmId;
            }
            set
            {
                this.PhoneticAlgorithmId = value;
            }
        }

    }

    /// <summary>
    /// Interface definition
    /// </summary>
    public partial class ConceptRelationshipType : IDbBaseData
    {
        /// <summary>
        /// Identification for object
        /// </summary>
        [LinqPropertyMap(nameof(ConceptRelationshipTypeId))]
        public Guid Id
        {
            get
            {
                return this.ConceptRelationshipTypeId;
            }
            set
            {
                this.ConceptRelationshipTypeId = value;
            }
        }

    }

    /// <summary>
    /// Interface definition
    /// </summary>
    public partial class CodeSystem : IDbBaseData
    {
        /// <summary>
        /// Identification for object
        /// </summary>
        [LinqPropertyMap(nameof(CodeSystemId))]
        public Guid Id
        {
            get
            {
                return this.CodeSystemId;
            }

            set
            {
                this.CodeSystemId = value;
            }
        }

    }

    /// <summary>
    /// Interface definition
    /// </summary>
    public partial class AssigningAuthority : IDbBaseData
    {
        /// <summary>
        /// Identification for object
        /// </summary>
        [LinqPropertyMap(nameof(AssigningAuthorityId))]
        public Guid Id
        {
            get
            {
                return this.AssigningAuthorityId;
            }

            set
            {
                this.AssigningAuthorityId = value;
            }
        }

    }

    /// <summary>
    /// Interface definition
    /// </summary>
    public partial class ConceptClass : IDbBaseData
    {
        /// <summary>
        /// Identification for object
        /// </summary>
        [LinqPropertyMap(nameof(ConceptClassId))]
        public Guid Id
        {
            get
            {
                return this.ConceptClassId;
            }

            set
            {
                this.ConceptClassId = value;
            }
        }

    }

    /// <summary>
    /// Interface definition
    /// </summary>
    public partial class ConceptSet : IDbBaseData
    {
        /// <summary>
        /// Identification for object
        /// </summary>
        [LinqPropertyMap(nameof(ConceptSetId))]
        public Guid Id
        {
            get
            {
                return this.ConceptSetId;
            }

            set
            {
                this.ConceptSetId = value;
            }
        }

    }
    /// <summary>
    /// Interface definition
    /// </summary>
    public partial class Policy : IDbBaseData
    {
        /// <summary>
        /// Identification for object
        /// </summary>
        [LinqPropertyMap(nameof(PolicyId))]
        public Guid Id
        {
            get
            {
                return this.PolicyId;
            }

            set
            {
                this.PolicyId = value;
            }
        }

    }

    /// <summary>
    /// Interface definition
    /// </summary>
    public partial class SecurityApplication : IDbBaseData
    {
        /// <summary>
        /// Identification for object
        /// </summary>
        [LinqPropertyMap(nameof(ApplicationId))]
        public Guid Id
        {
            get
            {
                return this.ApplicationId;
            }

            set
            {
                this.ApplicationId = value;
            }
        }

    }

    /// <summary>
    /// Interface definition for object
    /// </summary>
    public partial class SecurityDevice : IDbBaseData
    {
        /// <summary>
        /// Identification for object
        /// </summary>
        [LinqPropertyMap(nameof(DeviceId))]
        public Guid Id
        {
            get
            {
                return this.DeviceId;
            }

            set
            {
                this.DeviceId = value;
            }
        }

    }

    /// <summary>
    /// Interface definition for security user
    /// </summary>
    public partial class SecurityRole : IDbBaseData
    {
        /// <summary>
        /// Identification for object
        /// </summary>
        [LinqPropertyMap(nameof(RoleId))]
        public Guid Id
        {
            get
            {
                return this.RoleId;
            }

            set
            {
                this.RoleId = value;
            }
        }

    }

    /// <summary>
    /// Interface definition for security user
    /// </summary>
    public partial class SecurityUser : IDbBaseData
    {
        /// <summary>
        /// Identification for object
        /// </summary>
        [LinqPropertyMap(nameof(UserId))]
        public Guid Id
        {
            get
            {
                return this.UserId;
            }

            set
            {
                this.UserId = value;
            }
        }
        
    }
}
