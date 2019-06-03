using MARC.HI.EHRS.SVC.Messaging.FHIR.Backbone;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Handlers;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Resources;
using OpenIZ.Core.Model;
using OpenIZ.Core.Model.Collection;
using OpenIZ.Messaging.FHIR.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Reflection;
using OpenIZ.Core.Diagnostics;

namespace OpenIZ.Messaging.FHIR.Handlers
{
    /// <summary>
    /// Represents a FHIR resource handler for bundles
    /// </summary>
    public class BundleResourceHandler : RepositoryResourceHandlerBase<MARC.HI.EHRS.SVC.Messaging.FHIR.Resources.Bundle, OpenIZ.Core.Model.Collection.Bundle>
    {

        // Tracer
        private TraceSource m_tracer = new TraceSource("OpenIZ.Messaging.FHIR");

        /// <summary>
        /// Gets the interaction that this resource handler provider
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<InteractionDefinition> GetInteractions()
        {
            return new InteractionDefinition[]
            {
                new InteractionDefinition() { Type = TypeRestfulInteraction.Create },
                new InteractionDefinition() { Type = TypeRestfulInteraction.Update }
            };
        }


        /// <summary>
        /// Maps a OpenIZ bundle as FHIR
        /// </summary>
        protected override MARC.HI.EHRS.SVC.Messaging.FHIR.Resources.Bundle MapToFhir(Core.Model.Collection.Bundle model, WebOperationContext webOperationContext)
        {
            return new MARC.HI.EHRS.SVC.Messaging.FHIR.Resources.Bundle()
            {
                Type = BundleType.Collection,
                // TODO: Actually construct a response bundle 
            };
            
        }

        /// <summary>
        /// Map FHIR resource to our bundle
        /// </summary>
        protected override Core.Model.Collection.Bundle MapToModel(MARC.HI.EHRS.SVC.Messaging.FHIR.Resources.Bundle resource, WebOperationContext webOperationContext)
        {
            var retVal = new Core.Model.Collection.Bundle();
            foreach(var entry in resource.Entry)
            {
                var entryType = entry.Resource.Resource?.GetType();
                if (entryType == null)
                    continue;
                var handler = FhirResourceHandlerUtil.GetResourceHandler(entryType.GetCustomAttribute<XmlRootAttribute>().ElementName) as IBundleResourceHandler;
                if (handler == null)
                {
                    this.m_tracer.TraceWarning("Can't find bundle handler for {0}...", entryType.Name);
                    continue;
                }
                retVal.Add(handler.MapToModel(entry, webOperationContext, resource));
            }
            retVal.Item.RemoveAll(o => o == null);
            return retVal;
        }
    }
}
