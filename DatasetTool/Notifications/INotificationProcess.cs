using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OizDevTool.Notifications
{
    /// <summary>
    /// Notification process 
    /// </summary>
    public interface INotificationProcess
    {

        /// <summary>
        /// Process the specified notification 
        /// </summary>
        void Process(DateTime date, String facilityId, string language);
    }
}
