﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenIZ.Core.Model;
using OpenIZ.Core.Model.Query;
using OpenIZ.Core.Model.DataTypes;
using OpenIZ.Core.Services;
using MARC.HI.EHRS.SVC.Core;
using OpenIZ.Core.Model.Collection;

namespace OpenIZ.Messaging.IMSI.ResourceHandler
{
    /// <summary>
    /// Resource handler for concept sets
    /// </summary>
    public class ConceptSetResourceHandler : IResourceHandler
    {

        // Repository service
        private IConceptRepositoryService m_repositoryService; 

        /// <summary>
        /// Concept set ctor
        /// </summary>
        public ConceptSetResourceHandler()
        {
            ApplicationContext.Current.Started += (o, e) => this.m_repositoryService = ApplicationContext.Current.GetService<IConceptRepositoryService>();
        }

        /// <summary>
        /// Gets the resource name
        /// </summary>
        public string ResourceName
        {
            get
            {
                return "ConceptSet";
            }
        }

        /// <summary>
        /// Gets the type of serialization
        /// </summary>
        public Type Type
        {
            get
            {
                return typeof(ConceptSet);
            }
        }

        /// <summary>
        /// Creates the specified data
        /// </summary>
        public IdentifiedData Create(IdentifiedData data, bool updateIfExists)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            Bundle bundleData = data as Bundle;
            bundleData?.Reconstitute();
            var processData = bundleData?.Entry ?? data;

            if (processData is Bundle)
                throw new InvalidOperationException("Bundle must have entry of type ConceptSet");
            else if (processData is ConceptSet)
            {
                var conceptData = data as ConceptSet;
                if (updateIfExists)
                    return this.m_repositoryService.SaveConceptSet(conceptData);
                else
                    return this.m_repositoryService.InsertConceptSet(conceptData);
            }
            else
                throw new ArgumentException("Invalid persistence type");
        }

        /// <summary>
        /// Gets the specified conceptset
        /// </summary>
        public IdentifiedData Get(Guid id, Guid versionId)
        {
            if (versionId != Guid.Empty)
                throw new NotSupportedException();

            return this.m_repositoryService.GetConceptSet(id);

        }

        /// <summary>
        /// Obsolete the specified concept set
        /// </summary>
        public IdentifiedData Obsolete(Guid key)
        {
            return this.m_repositoryService.ObsoleteConceptSet(key);
        }

        /// <summary>
        /// Perform query 
        /// </summary>
        public IEnumerable<IdentifiedData> Query(NameValueCollection queryParameters)
        {
            return this.m_repositoryService.FindConceptSets(QueryExpressionParser.BuildLinqExpression<ConceptSet>(queryParameters));
        }

        /// <summary>
        /// Query with specified parameter data
        /// </summary>
        public IEnumerable<IdentifiedData> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            return this.m_repositoryService.FindConceptSets(QueryExpressionParser.BuildLinqExpression<ConceptSet>(queryParameters), offset, count, out totalCount);
        }

        /// <summary>
        /// Update the specified object
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public IdentifiedData Update(IdentifiedData data)
        {

            Bundle bundleData = data as Bundle;
            bundleData?.Reconstitute();
            var processData = bundleData?.Entry ?? data;

            if (processData is Bundle)
                throw new InvalidOperationException("Bundle must have entry of type Concept");
            else if (processData is ConceptSet)
            {
                var conceptData = data as ConceptSet;
                return this.m_repositoryService.SaveConceptSet(conceptData);
            }
            else
                throw new ArgumentException("Invalid persistence type");
        }
    }
}
