using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Backbone;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Resources;
using OpenIZ.Core.Model;
using OpenIZ.Core.Model.Constants;
using OpenIZ.Core.Model.DataTypes;
using OpenIZ.Core.Model.Entities;
using OpenIZ.Messaging.FHIR.Util;

namespace OpenIZ.Messaging.FHIR.Handlers
{
    /// <summary>
    /// Organization resource provider
    /// </summary>
    public class OrganizationResourceHandler : RepositoryResourceHandlerBase<MARC.HI.EHRS.SVC.Messaging.FHIR.Resources.Organization, OpenIZ.Core.Model.Entities.Organization>, IBundleResourceHandler
    {

        /// <summary>
        /// Map to model
        /// </summary>
        public IdentifiedData MapToModel(BundleEntry bundleResource, WebOperationContext context, Bundle bundle)
        {
            return this.MapToModel(bundleResource.Resource.Resource as MARC.HI.EHRS.SVC.Messaging.FHIR.Resources.Organization, context);
        }

        /// <summary>
        /// Get the interactions 
        /// </summary>
        protected override IEnumerable<InteractionDefinition> GetInteractions()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Map to FHIR
        /// </summary>
        protected override MARC.HI.EHRS.SVC.Messaging.FHIR.Resources.Organization MapToFhir(Core.Model.Entities.Organization model, WebOperationContext webOperationContext)
        {
            return DataTypeConverter.CreateResource<MARC.HI.EHRS.SVC.Messaging.FHIR.Resources.Organization>(model);
        }

        /// <summary>
        /// Map to Model
        /// </summary>
        protected override Core.Model.Entities.Organization MapToModel(MARC.HI.EHRS.SVC.Messaging.FHIR.Resources.Organization resource, WebOperationContext webOperationContext)
        {
            // Organization
            var retVal = new Core.Model.Entities.Organization()
            {
                TypeConcept = DataTypeConverter.ToConcept(resource.Type),
                Addresses = resource.Address.Select(DataTypeConverter.ToEntityAddress).ToList(),
                CreationTime = DateTimeOffset.Now,
                // TODO: Extensions
                Extensions = resource.Extension.Select(DataTypeConverter.ToEntityExtension).OfType<EntityExtension>().ToList(),
                Identifiers = resource.Identifier.Select(DataTypeConverter.ToEntityIdentifier).ToList(),
                Key = Guid.NewGuid(),
                Names = new List<EntityName>() { new EntityName(NameUseKeys.OfficialRecord, resource.Name) },
                StatusConceptKey = resource.Active?.Value == true ? StatusKeys.Active : StatusKeys.Obsolete,
                Telecoms = resource.Telecom.Select(DataTypeConverter.ToEntityTelecomAddress).ToList()
            };

            Guid key;
            if (!Guid.TryParse(resource.Id, out key))
            {
                key = Guid.NewGuid();
            }

            retVal.Key = key;

            return retVal;
        }
    }
}
