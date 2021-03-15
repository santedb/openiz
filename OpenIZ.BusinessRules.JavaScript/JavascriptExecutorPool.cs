/*
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE.md)
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
 * Date: 2021-2-9
 */
using OpenIZ.Core;
using OpenIZ.Core.Model;
using OpenIZ.Core.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace OpenIZ.BusinessRules.JavaScript
{
    /// <summary>
    /// Represents a pool of javascript executors which can be run/executed when they are not busy
    /// </summary>
    public class JavascriptExecutorPool : IDisposable
    {

        // Get current thread pool
        private static JavascriptExecutorPool s_current;

        // Lock
        private object m_lock = new object();

        // Executor pools
        private JavascriptExecutor[] m_executors = null;

        // Executors that are free
        private ConcurrentStack<JavascriptExecutor> m_freeExecutors = new ConcurrentStack<JavascriptExecutor>();

        // Reset event
        private ManualResetEventSlim m_resetEvent = new ManualResetEventSlim(true);

        /// <summary>
        /// Executes the specified trigger on an available queue and waits for the result
        /// </summary>
        public Object Execute<TData>(Func<JavascriptExecutor, TData, Object> action, TData data) where TData : IdentifiedData
        {
            // Attempt to pull a free worker off the queue
            JavascriptExecutor worker = null;
            while (!this.m_freeExecutors.TryPop(out worker))
            {
                this.m_resetEvent.Wait();
                if (this.m_freeExecutors == null)
                    throw new ObjectDisposedException("This worker pool has been disposed");
                this.m_resetEvent.Reset();
            }

            try
            {
                return action(worker, data);
            }
            finally
            {
                // Free the worker
                this.m_freeExecutors.Push(worker) ;
                this.m_resetEvent.Set(); // notify free
            }
        }

        /// <summary>
        /// Execute the specified script on all executors
        /// </summary>
        /// <remarks>Note: The executors may be busy executing scripts</remarks>
        public void ExecuteGlobal(Action<JavascriptExecutor> exec)
        {
            foreach(var itm in this.m_executors)
                exec(itm);
        }

        /// <summary>
        /// Dispose of this pool
        /// </summary>
        public void Dispose()
        {
            // Don't allow anyone to execute
            this.m_freeExecutors.Clear();
            foreach (var i in this.m_executors)
                i.Dispose();
            this.m_executors = null;
            this.m_freeExecutors = null;

            // Notify 
            this.m_resetEvent.Set();
            s_current = null;
        }

        /// <summary>
        /// Creates a new instance of the wait thread pool
        /// </summary>
        private JavascriptExecutorPool()
        {
            var concurrency = Environment.ProcessorCount;
            this.m_executors = new JavascriptExecutor[concurrency];
            for (int i = 0; i < concurrency; i++)
            {
#if DEBUG
                this.m_executors[i] = new JavascriptExecutor(true);
#else
                this.m_executors[i] = new JavascriptExecutor(false);
#endif
                this.m_freeExecutors.Push(this.m_executors[i]);
            }
        }


        /// <summary>
        /// Javascript executor pool
        /// </summary>
        static JavascriptExecutorPool ()
        {
            s_current = new JavascriptExecutorPool();
        }

        /// <summary>
        /// Get the singleton threadpool
        /// </summary>
        public static JavascriptExecutorPool Current
        {
            get
            {
                return s_current;
            }
        }

    }
}
