/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: fyfej
 * Date: 2017-9-1
 */
using MARC.Everest.Threading;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using OpenIZ.Core.Configuration;
using OpenIZ.Core.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenIZ.Core.Services.Impl
{
    /// <summary>
    /// Represents a thread pool service
    /// </summary>
    [Description("OpenIZ PCL ThreadPool Provider")]
    public class ThreadPoolService : IDisposable, IThreadPoolService
    {

        // Lock
        private object s_lock = new object();

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(ThreadPoolService));

        // Timers
        private List<Timer> m_timers = new List<Timer>();

        // Number of threads to keep alive
        private int m_concurrencyLevel = System.Environment.ProcessorCount * 4;

        // Queue of work items
        private ConcurrentQueue<WorkItem> m_queue = null;

        // Active threads
        private Thread[] m_threadPool = null;

        // True when the thread pool is being disposed
        private bool m_disposing = false;

        // Reset event
        private ManualResetEventSlim m_resetEvent = new ManualResetEventSlim(false);

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Legacy Thread Pool Service";

        /// <summary>
        /// Creates a new instance of the wait thread pool
        /// </summary>
        public ThreadPoolService()
        {
            this.EnsureStarted(); // Ensure thread pool threads are started
            this.m_queue = new ConcurrentQueue<WorkItem>();
        }

        /// <summary>
        /// Worker data structure
        /// </summary>
        private struct WorkItem
        {
            /// <summary>
            /// The callback to execute on the worker
            /// </summary>
            public Action<Object> Callback { get; set; }
            /// <summary>
            /// The state or parameter to the worker
            /// </summary>
            public object State { get; set; }
            /// <summary>
            /// The execution context
            /// </summary>
            public ExecutionContext ExecutionContext { get; set; }
        }

        /// <summary>
        /// Queue a work item to be completed
        /// </summary>
        public void QueueUserWorkItem(Action<Object> callback)
        {
            QueueUserWorkItem(callback, null);
        }

        /// <summary>
        /// Queue a user work item with the specified parameters
        /// </summary>
        public void QueueUserWorkItem(Action<Object> callback, object state)
        {
            this.QueueWorkItemInternal(callback, state);
        }

        /// <summary>
        /// Perform queue of workitem internally
        /// </summary>
        private void QueueWorkItemInternal(Action<Object> callback, object state)
        {
            ThrowIfDisposed();

            try
            {
                WorkItem wd = new WorkItem()
                {
                    Callback = callback,
                    State = state,
                    ExecutionContext = ExecutionContext.Capture()
                };

                m_queue.Enqueue(wd);
                this.m_resetEvent.Set();
            }
            catch (Exception e)
            {
                try
                {
                    this.m_tracer.TraceError("Error queueing work item: {0}", e);
                }
                catch { }
            }
        }

        /// <summary>
        /// Ensure the thread pool threads are started
        /// </summary>
        private void EnsureStarted()
        {
            // Load configuration
            this.m_concurrencyLevel = (ApplicationContext.Current.GetService<IConfigurationManager>().GetSection("openiz.core") as OpenIzConfiguration)?.ThreadPoolSize ?? this.m_concurrencyLevel;
            m_threadPool = new Thread[m_concurrencyLevel];
            for (int i = 0; i < m_threadPool.Length; i++)
            {
                m_threadPool[i] = this.CreateThreadPoolThread();
                m_threadPool[i].Start();
            }
        }

        /// <summary>
        /// Create a thread pool thread
        /// </summary>
        private Thread CreateThreadPoolThread()
        {
            return new Thread(this.DispatchLoop)
            {
                Name = String.Format("RSRVR-ThreadPoolThread"),
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal
            };
        }

        /// <summary>
        /// Dispatch loop
        /// </summary>
        private void DispatchLoop()
        {
            while (!this.m_disposing)
            {
                try
                {
                    this.m_resetEvent.Wait();
                    while (this.m_queue.TryDequeue(out WorkItem wi))
                    {
                        wi.Callback(wi.State);
                    }
                    this.m_resetEvent.Reset();
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error in dispatchloop {0}", e);
                }
            }
        }

        /// <summary>
        /// Throw an exception if the object is disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (this.m_disposing) throw new ObjectDisposedException(nameof(ThreadPoolService));
        }


        /// <summary>
        /// Dispose the object
        /// </summary>
        public void Dispose()
        {

            if (this.m_disposing) return;

            this.m_disposing = true;

            this.m_resetEvent.Set();

            if (m_threadPool != null)
            {
                for (int i = 0; i < m_threadPool.Length; i++)
                {
                    if (!m_threadPool[i].Join(1000))
                        m_threadPool[i].Abort();
                    m_threadPool[i] = null;
                }
            }
        }

        /// <summary>
        /// Queue a work item on a timeout
        /// </summary>
        public void QueueUserWorkItem(TimeSpan timeout, Action<object> action, object parm)
        {
            // Use timer service if it is available
            Timer timer = null;
            timer = new Timer((o) => {
                try
                {
                    this.m_tracer.TraceVerbose("TIMER THREAD START: {0}({1})", action, o);
                    action(o);
                    this.m_tracer.TraceVerbose("TIMER THREAD STOP: {0}({1})", action, o);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("THREAD DEATH: {0}", e);

                }
                finally
                {
                    this.m_timers.Remove(timer);
                }
            }, parm, (int)timeout.TotalMilliseconds, Timeout.Infinite);
            this.m_timers.Add(timer);
        }

        /// <summary>
        /// Queue a non-pooled item
        /// </summary>
        public void QueueNonPooledWorkItem(Action<object> action, object parm)
        {
            this.QueueUserWorkItem(action, parm);
        }
    }
}
