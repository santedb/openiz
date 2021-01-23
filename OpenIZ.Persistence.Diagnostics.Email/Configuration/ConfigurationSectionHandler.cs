using System;
using System.Configuration;
using System.Linq;
using System.Xml;

namespace OpenIZ.Persistence.Diagnostics.Email.Configuration
{
	/// <summary>
	/// Configuration section handler
	/// </summary>
	public class ConfigurationSectionHandler : IConfigurationSectionHandler
	{
		/// <summary>
		/// Creates the specified configuration object
		/// </summary>
		public object Create(object parent, object configContext, XmlNode section)
		{
			
            var recipients = section.SelectNodes("./*[local-name() = 'recipient']/*[local-name() = 'add']");

            DiagnosticEmailServiceConfiguration retVal = new DiagnosticEmailServiceConfiguration();

            retVal.Recipients = new System.Collections.Generic.List<string>();
            foreach (XmlElement itm in recipients)
                retVal.Recipients.Add(itm.InnerText);
            return retVal;
		}
	}
}