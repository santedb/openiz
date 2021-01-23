using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using MARC.HI.EHRS.SVC.Core.Timer;

namespace OpenIZ.Core.Services.Jobs
{
    /// <summary>
    /// Collection compact job that cleans the server memory and compacts the GC Generation 2 LOB 
    /// </summary>
    public class GarbageCollectionCompactJob : ITimerJob
    {

        /// <summary>
        /// Fired when the job elapses
        /// </summary>
        public void Elapsed(object sender, ElapsedEventArgs e)
        {
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(2, GCCollectionMode.Forced);
        }
    }
}
