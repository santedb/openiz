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
using Jint.Runtime;
using OpenIZ.BusinessRules.JavaScript.Exceptions;
using OpenIZ.BusinessRules.JavaScript.JNI;
using OpenIZ.BusinessRules.JavaScript.Util;
using OpenIZ.Core;
using OpenIZ.Core.Diagnostics;
using OpenIZ.Core.Exceptions;
using OpenIZ.Core.Model;
using OpenIZ.Core.Model.Query;
using OpenIZ.Core.Model.Serialization;
using OpenIZ.Core.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static Jint.Engine;

namespace OpenIZ.BusinessRules.JavaScript
{
    /// <summary>
    /// A class that can execute JavaScript functions on a particular thread
    /// </summary>
    public class JavascriptExecutor : IDisposable
    {
        /// <summary>
        /// Callback comparer
        /// </summary>
        private class JavascriptCallbackComparer : IEqualityComparer<JavascriptCallbackInfo>
        {
            /// <summary>
            /// Determine equality
            /// </summary>
            public bool Equals(JavascriptCallbackInfo x, JavascriptCallbackInfo y)
            {
                return x?.Id == y?.Id;
            }

            /// <summary>
            /// Get the hash code
            /// </summary>
            public int GetHashCode(JavascriptCallbackInfo obj)
            {
                return obj?.Id?.GetHashCode() ?? 0;
            }
        }

        /// <summary>
        /// Information about a JS callback
        /// </summary>
        private class JavascriptCallbackInfo
        {

            /// <summary>
            /// Creates a new callback info structure
            /// </summary>
            public JavascriptCallbackInfo(String id, String trigger, NameValueCollection guard, Delegate callback)
            {
                this.Id = id;
                this.TriggerName = trigger;
                this.Guard = guard;
                this.Callback = callback;
            }

            /// <summary>
            /// Creates a new callback info structure
            /// </summary>
            public JavascriptCallbackInfo(String id, String trigger, Delegate callback) : this(id, trigger, null, callback)
            {
            }

            /// <summary>
            /// Creates a new callback info structure
            /// </summary>
            public JavascriptCallbackInfo(String trigger, Delegate callback) : this(Guid.NewGuid().ToString(), trigger, null, callback)
            {
            }


            /// <summary>
            /// Gets the id of the callback
            /// </summary>
            public string Id { get; }

            /// <summary>
            /// Gets the trigger name
            /// </summary>
            public string TriggerName { get; }

            /// <summary>
            /// Gets or sets the guard
            /// </summary>
            public NameValueCollection Guard { get; }

            /// <summary>
            /// Gets the callback
            /// </summary>
            public Delegate Callback { get; }

        }


        // Comparer
        private IEqualityComparer<JavascriptCallbackInfo> m_javascriptComparer = new JavascriptCallbackComparer();

        // Serializer binder
        private ModelSerializationBinder m_binder = new ModelSerializationBinder();

        // Locking for thread
        private object m_lock = new object();

        // Javascript callback information
        private Dictionary<Type, List<JavascriptCallbackInfo>> m_registeredCallback = new Dictionary<Type, List<JavascriptCallbackInfo>>();

        // Already executed rules
        private HashSet<String> m_executed = new HashSet<string>();

        // The actual Javascript engine
        private Jint.Engine m_engine;

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(JavascriptExecutor));

        /// <summary>
        /// Gets the current engine
        /// </summary>
        public Jint.Engine Engine => this.m_engine;

        /// <summary>
        /// Creates a new javascript execution engine
        /// </summary>
        /// <param name="debugMode">True if the engine should be started in debug mode</param>
        internal JavascriptExecutor(bool debugMode)
        {
            this.m_engine = new Jint.Engine(cfg => cfg.AllowClr(
                typeof(OpenIZ.Core.Model.BaseEntityData).GetTypeInfo().Assembly,
                typeof(IBusinessRulesService<>).GetTypeInfo().Assembly
                )
            .Strict(false)
            .CatchClrExceptions(o => !(o is DetectedIssueException) && !(o is JsBusinessRuleException))
            .DebugMode(debugMode))
                .SetValue("OpenIZBre", new JavascriptEngineBridge(this))
                .SetValue("console", new JsConsoleProvider())
                .SetValue("reflector", new JsObjectProvider());

            // Add embedded javascript files into the object
            foreach (var itm in typeof(JavascriptExecutor).GetTypeInfo().Assembly.GetManifestResourceNames().Where(o => o.EndsWith(".js")))
                using (StreamReader sr = new StreamReader(typeof(JavascriptExecutor).GetTypeInfo().Assembly.GetManifestResourceStream(itm)))
                    this.ExecuteScript(itm.Replace(typeof(JavascriptExecutor).GetTypeInfo().Assembly.FullName, ""), sr.ReadToEnd());

        }

        /// <summary>
        /// Adds an object which is exposed to the BRE layer
        /// </summary>
        public void AddExposedObject(String identifier, Object jniObject)
        {
            lock (this.m_lock)
                this.m_engine.SetValue(identifier, jniObject);
        }


        /// <summary>
        /// Executes the specified script contents
        /// </summary>
        public void ExecuteScript(String scriptId, String script)
        {
            try
            {
                // Already ran
                if (!this.m_executed.Contains(scriptId))
                {
                    this.m_tracer.TraceVerbose("Adding rules to BRE: {0}", scriptId);
                    // Find all reference paths
                    Regex includeReg = new Regex(@"\/\/\/\s*?\<reference\s*?path\=[""'](.*?)[""']\s?\/\>", RegexOptions.Multiline);
                    var incMatches = includeReg.Matches(script);

                    foreach (Match match in incMatches)
                    {
                        var include = match.Groups[1].Value;
                        var incStream = (ApplicationServiceContext.Current.GetService(typeof(IDataReferenceResolver)) as IDataReferenceResolver)?.Resolve(include);
                        if (incStream == null)
                            this.m_tracer.TraceWarning("Include {0} not found", include);
                        else
                            try
                            {
                                using (StreamReader sr = new StreamReader(incStream))
                                    this.m_engine.Execute(sr.ReadToEnd());
                            }
                            catch (Exception e)
                            {
                                this.m_tracer.TraceWarning("Ich bin der roboter: Will skip {0} due to {1}", include, e.Message);
                            }
                    }

                    this.m_executed.Add(scriptId);
                    lock (this.m_lock) // Lock while executing
                        this.m_engine.Execute(script);
                }
                else
                    this.m_tracer.TraceInfo("Script {0} has already been run", scriptId);
            }
            catch (JavaScriptException ex)
            {
                this.m_tracer.TraceError("Ich bin der roboter: Error executing JavaScript {0}:{1} > {2}", ex.LineNumber, ex.Column, ex);
                throw ex;
            }
        }

        /// <summary>
        /// Registers the specified callback using the type name
        /// </summary>
        public void RegisterCallback(String id, String targetResource, String trigger, NameValueCollection guard, Func<dynamic, dynamic> _delegate)
        {
            var type = this.m_binder.BindToType(null, targetResource);
            if (type == null)
                throw new InvalidOperationException($"Could not find resource type registration {targetResource}");
            this.RegisterCallback(id, type, trigger, guard, _delegate);
        }
        /// <summary>
        /// Register the specified rule with this engine
        /// </summary>
        public void RegisterCallback(String id, Type targetType, String trigger, NameValueCollection guard, Func<dynamic, dynamic> _delegate)
        {
            lock (this.m_lock) // synchronous method to prevent duplicate threads from registering the same callback
            {
                // Get current locks
                if (!this.m_registeredCallback.TryGetValue(targetType, out List<JavascriptCallbackInfo> callbacks))
                {
                    callbacks = new List<JavascriptCallbackInfo>();
                    this.m_registeredCallback.Add(targetType, callbacks);
                }

                if (callbacks.Any(o => o.Id == id && o.TriggerName == trigger))
                    this.m_tracer.TraceWarning("{0} rule {1} has already been registered with this engine", trigger, id);
                else
                {
                    // Register a BRE hook
                    if (!this.IsRegistered(targetType))
                        ApplicationServiceContext.Current.AddBusinessRule(typeof(JavascriptBusinessRule<>).MakeGenericType(targetType));

                    callbacks.Add(new JavascriptCallbackInfo(id, trigger, guard, _delegate));
                }
            }
        }

        /// <summary>
        /// True if the JS handler is registered
        /// </summary>
        private bool IsRegistered(Type targetType)
        {
            var jsBreType = typeof(JavascriptBusinessRule<>).MakeGenericType(targetType);
            var bre = ApplicationServiceContext.Current.GetService(jsBreType) as IBusinessRulesService;
            if (bre == null) // scan deep
            {
                bre = ApplicationServiceContext.Current.GetService(typeof(IBusinessRulesService<>).MakeGenericType(targetType)) as IBusinessRulesService;
                while (bre?.Next != null)
                {
                    if (bre.Next.GetType() == jsBreType)
                        return true;
                    bre = bre.Next;
                }
                return false;
            }
            else
                return true;
        }
        /// <summary>
        /// Execute the appropriate callbacks
        /// </summary>
        private IEnumerable<JavascriptCallbackInfo> GetCallList<TBinding>(String trigger)
        {
            return this.GetCallList(typeof(TBinding), trigger);
        }

        /// <summary>
        /// Get the call list for the specified type binding
        /// </summary>
        private IEnumerable<JavascriptCallbackInfo> GetCallList(Type tBinding, String trigger)
        {
            if (this.m_registeredCallback.TryGetValue(tBinding, out List<JavascriptCallbackInfo> retVal))
                return retVal.Where(o => o.TriggerName == trigger);
            else
                return new JavascriptCallbackInfo[0];
        }

        /// <summary>
        /// Evaluates the guard condition for the specified object
        /// </summary>
        /// <param name="guard">The guard that is to be evaluated</param>
        /// <param name="data">The data to be evaluated against</param>
        /// <returns>The evaluation criteria</returns>
        private bool GuardEval(NameValueCollection guard, IDictionary<String, Object> data)
        {
            var retVal = true;
            foreach (var gc in guard)
            {
                if (gc.Key.Contains(".") || gc.Key.Contains("["))
                    throw new InvalidOperationException("Rule guards can only be simple property paths");
                if (gc.Key.StartsWith("_"))
                    continue; // ignore control parms
                bool subCond = false;
                foreach (var v in gc.Value)
                {
                    if (gc.Value.First() == "null")
                        subCond |= !data.ContainsKey(gc.Key) || data[gc.Key] == null;
                    else if (data.TryGetValue(gc.Key, out object value))
                        subCond |= value.Equals(v);
                    else
                        subCond = false;
                }
                retVal &= subCond;
            }
            return retVal;
        }

        /// <summary>
        /// Invoke raw
        /// </summary>
        public object InvokeRaw(String triggerName, Object data)
        {
            lock (this.m_lock) // Only one object can use this thread at a time
            {

                var sdata = data as IDictionary<String, Object>;
                if (sdata == null || !sdata.ContainsKey("$type")) return data;
                var callList = this.GetCallList(this.m_binder.BindToType("OpenIZ.Core.Model, Version=1.1.0.0", sdata["$type"].ToString()), triggerName);
                var retVal = data;

                if (callList.Any())
                {
                    foreach (var c in callList)
                    {
                        try
                        {
                            if (c.Guard == null || this.GuardEval(c.Guard, sdata))
                                data = c.Callback.DynamicInvoke(data);
                        }
                        catch (JavaScriptException e)
                        {
                            this.m_tracer.TraceError("JAVASCRIPT ERROR RUNNING {0} OBJECT :::::> {3}@{2}\r\n{1}", triggerName, JavascriptUtils.ProduceLiteral(data), e.LineNumber, e);
                            throw new DetectedIssueException(new List<DetectedIssue>()
                            {
                                new DetectedIssue()
                                {
                                    Priority = DetectedIssuePriorityType.Error,
                                    Text = $"Error executing {triggerName} (rule id: {c.Id}) {e.Error.ToString()} @ {e.Location.Start.Line} - {e.Location.End.Line}"
                                }
                            });

                        }
                        catch (Exception e)
                        {
                            this.m_tracer.TraceError("Error running {0} for {1} : {2}", triggerName, JavascriptUtils.ProduceLiteral(data), e);
                            throw new JsBusinessRuleException($"Error running business rule {triggerName} for {JavascriptUtils.ProduceLiteral(data)} - {e.Message}", e);
                        }

                    }
                }

                return data;
            }

        }

        /// <summary>
        /// Perform actual invokation on all objects
        /// </summary>
        public TBinding Invoke<TBinding>(string triggerName, TBinding data) where TBinding : IdentifiedData
        {
            lock (this.m_lock)
            {


                if (data == default(TBinding)) return data;

                var callList = this.GetCallList(data.GetType(), triggerName);
                callList = callList.Union(this.GetCallList<TBinding>(triggerName), this.m_javascriptComparer).ToList();
                var retVal = data;

                if (callList.Count() > 0)
                {
                    dynamic viewModel = JavascriptUtils.ToViewModel(retVal);
                    foreach (var c in callList)
                    {
                        try
                        {
                            // There is a guard so let's execute it
                            if (c.Guard == null || QueryExpressionParser.BuildLinqExpression<TBinding>(c.Guard).Compile()(data))
                            {
                                viewModel = c.Callback.DynamicInvoke(viewModel);
                            }
                        }
                        catch (JavaScriptException e)
                        {
                            this.m_tracer.TraceError("JS ERROR: Error running {0} for {1} @ {2}:{3} \r\n Javascript Stack: {4} \r\n C# Stack: {5}",
                                triggerName, data, e.Location.Source, e.LineNumber, e.CallStack, e);
                            throw new JsBusinessRuleException($"Error running business rule {c.Id} - {triggerName} for {data}", e);
                        }
                        catch (Exception e)
                        {
                            this.m_tracer.TraceError("Error running {0} for {1} : {2}", triggerName, data, e);
                            throw new JsBusinessRuleException($"Error running business rule {c.Id} - {triggerName} for {data}", e);
                        }
                    }
                    retVal = (TBinding)JavascriptUtils.ToModel(viewModel);
                }

                return retVal;

            }
        }

        /// <summary>
        /// Validate the data object if validation is available
        /// </summary>
        public List<DetectedIssue> Validate<TBinding>(TBinding data) where TBinding : IdentifiedData
        {
            lock (this.m_lock)
            {

                var callList = this.GetCallList(data.GetType(), "Validate").Union(this.GetCallList<TBinding>("Validate"), this.m_javascriptComparer).Distinct();
                var retVal = new List<DetectedIssue>();
                var vmData = JavascriptUtils.ToViewModel(data);
                foreach (var c in callList)
                {
                    try
                    {
                        object[] issues = null;
                        issues = c.Callback.DynamicInvoke(vmData) as object[];
                        retVal.AddRange(issues.Cast<IDictionary<String, Object>>().Select(o => new DetectedIssue()
                        {
                            Text = o.ContainsKey("text") ? o["text"]?.ToString() : null,
                            Priority = o.ContainsKey("priority") ? (DetectedIssuePriorityType)(int)(double)o["priority"] : DetectedIssuePriorityType.Informational,
                            TypeKey = o.ContainsKey("type") ? Guid.Parse(o["type"].ToString()) : Guid.Empty
                        }));
                    }
                    catch (JavaScriptException e)
                    {
                        this.m_tracer.TraceError("Error validating {0} (rule: {1}) : {2}", data, c.Id, e);
                        return new List<DetectedIssue>()
                            {
                                new DetectedIssue()
                                {
                                    Priority = DetectedIssuePriorityType.Error,
                                    Text = $"Error validating {data} (rule: {c.Id}) - {e.Message} @ {e.LineNumber}"
                                }
                            };

                    }
                    catch (Exception e)
                    {
                        this.m_tracer.TraceError("Error validating {0} (rule: {1}) : {2}", data, c.Id, e);
                        return new List<DetectedIssue>()
                            {
                                new DetectedIssue()
                                {
                                    Priority = DetectedIssuePriorityType.Error,
                                    Text = $"Error validating {data} (rule: {c.Id}) - {e.Message}"
                                }
                            };
                    }
                }
                return retVal;

            }
        }

        /// <summary>
        /// Dispose this object
        /// </summary>
        public void Dispose()
        {
            this.m_engine.BreakPoints.Clear();
            this.m_registeredCallback.Clear();
            this.m_registeredCallback = null;
            this.m_engine = null;
        }
    }
}
