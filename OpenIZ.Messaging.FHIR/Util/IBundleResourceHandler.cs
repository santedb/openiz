using MARC.HI.EHRS.SVC.Messaging.FHIR.Backbone;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Resources;
using OpenIZ.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Messaging.FHIR.Util
{
    /// <summary>
    /// Represents a bundle resource handler
    /// </summary>
    public interface IBundleResourceHandler
    {

        /// <summary>
        /// Maps the specified bundle entry resource to an identified data entry
        /// </summary>
        IdentifiedData MapToModel(BundleEntry bundleResource, WebOperationContext context, Bundle bundle);

    }
}
