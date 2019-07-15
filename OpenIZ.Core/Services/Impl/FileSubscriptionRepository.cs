using MARC.HI.EHRS.SVC.Core.Services;
using OpenIZ.Core.Diagnostics;
using OpenIZ.Core.Interfaces;
using OpenIZ.Core.Model.Subscription;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Core.Services.Impl
{
    /// <summary>
    /// An implementation of the ISubscriptionRepository that loads definitions from applets
    /// </summary>
    /// <remarks>
    /// This class is a backport of the SanteDB repsitory service for subscription definitions
    /// </remarks>
    public class FileSubscriptionRepository : IRepositoryService<SubscriptionDefinition>, IDaemonService
    {

        // Subscription definitions
        private List<SubscriptionDefinition> m_subscriptionDefinitions;

        // Lock object
        private object m_lockObject = new object();

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(FileSubscriptionRepository));

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "File Based Server Subscription Manager";

        /// <summary>
        /// Returns true if this service is running
        /// </summary>
        public bool IsRunning => this.m_subscriptionDefinitions != null;

        /// <summary>
        /// Fired when the service is starting
        /// </summary>
        public event EventHandler Starting;
        /// <summary>
        /// Fired when the service has started
        /// </summary>
        public event EventHandler Started;
        /// <summary>
        /// Fired when the service is about to stop
        /// </summary>
        public event EventHandler Stopping;
        /// <summary>
        /// Fired when the service has stopped
        /// </summary>
        public event EventHandler Stopped;
        public event EventHandler<AuditDataEventArgs> DataCreated;
        public event EventHandler<AuditDataEventArgs> DataUpdated;
        public event EventHandler<AuditDataEventArgs> DataObsoleted;
        public event EventHandler<AuditDataDisclosureEventArgs> DataDisclosed;

        /// <summary>
        /// Find the specified object
        /// </summary>
        public IEnumerable<SubscriptionDefinition> Find(Expression<Func<SubscriptionDefinition, bool>> query)
        {
            int tr;
            return this.Find(query, 0, 100, out tr);
        }

        /// <summary>
        /// Find the specified subscription definitions
        /// </summary>
        public IEnumerable<SubscriptionDefinition> Find(Expression<Func<SubscriptionDefinition, bool>> query, int offset, int? count, out int totalResults)
        {
            var results = this.m_subscriptionDefinitions?.Where(query.Compile());
            totalResults = results.Count();
            return results.Skip(offset).Take(count ?? 100);
        }

        /// <summary>
        /// Get the specified definition
        /// </summary>
        public SubscriptionDefinition Get(Guid key)
        {
            return this.m_subscriptionDefinitions.FirstOrDefault(o => o.Key == key);
        }

        /// <summary>
        /// Gets the specified definition
        /// </summary>
        public SubscriptionDefinition Get(Guid key, Guid versionKey)
        {
            return this.Get(key);
        }

        /// <summary>
        /// Insert the specified definition
        /// </summary>
        public SubscriptionDefinition Insert(SubscriptionDefinition data)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Obsolete the specified definition
        /// </summary>
        public SubscriptionDefinition Obsolete(Guid key)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Save the specified definition
        /// </summary>
        public SubscriptionDefinition Save(SubscriptionDefinition data)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Start the service
        /// </summary>
        public bool Start()
        {
            this.Starting?.Invoke(this, EventArgs.Empty);

            this.m_subscriptionDefinitions = new List<SubscriptionDefinition>();

            this.m_tracer.TraceInfo("Starting FileSystem based subscription definition repository");
            var dir = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "subscription");

            foreach(var fil in Directory.GetFiles(dir, "*.xml"))
            {
                this.m_tracer.TraceInfo("Loading {0}", fil);
                try
                {
                    using (var f = File.OpenRead(fil))
                        this.m_subscriptionDefinitions.Add(SubscriptionDefinition.Load(f));

                }
                catch(Exception e)
                {
                    this.m_tracer.TraceError("Error loading {0}: {1}", fil, e);
                }
            }

            this.Started?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Stop the service
        /// </summary>
        public bool Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);
            this.m_subscriptionDefinitions = null;
            this.Stopped?.Invoke(this, EventArgs.Empty);
            return true;
        }
    }
}
