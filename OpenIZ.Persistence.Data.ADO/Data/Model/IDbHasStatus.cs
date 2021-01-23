using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Persistence.Data.ADO.Data.Model
{
    /// <summary>
    /// Represents a class which has a status
    /// </summary>
    public interface IDbHasStatus
    {

        /// <summary>
        /// Gets or sets the status key
        /// </summary>
        Guid StatusConceptKey { get; set; }
    }
}
