using OpenIZ.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Core.Services
{
    /// <summary>
    /// Data Archive service
    /// </summary>
    public interface IDataArchiveService
    {
        /// <summary>
        /// Push the specified records to the archive
        /// </summary>
        void Archive(Type modelType, params Guid[] keysToBeArchived);

    }
}
