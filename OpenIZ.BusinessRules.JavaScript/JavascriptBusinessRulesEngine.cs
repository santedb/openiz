﻿/*
 * Copyright 2015-2017 Mohawk College of Applied Arts and Technology
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
 * User: justi
 * Date: 2016-11-8
 */
using Jint.Native;
using Jint.Runtime;
using Newtonsoft.Json;
using OpenIZ.BusinessRules.JavaScript.JNI;
using OpenIZ.Core;
using OpenIZ.Core.Diagnostics;
using OpenIZ.Core.Model.Acts;
using OpenIZ.Core.Services;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Jint;
using OpenIZ.Core.Model;
using System.Text.RegularExpressions;
using OpenIZ.Core.Interfaces;

namespace OpenIZ.BusinessRules.JavaScript
{
    /// <summary>
    /// Represents the JavaScript business rules engine
    /// </summary>
    public class JavascriptBusinessRulesEngine
    {

        // Tracer for JSBRE
        private Tracer m_tracer = Tracer.GetTracer(typeof(JavascriptBusinessRulesEngine));

        // Javascript BRE instance
        private static JavascriptBusinessRulesEngine s_instance;

        // Sync lock
        private static Object s_syncLock = new object();

        // Javascript engine
        private Jint.Engine m_engine = null;

        // Bridge
        private JNI.BusinessRulesBridge m_bridge = new JNI.BusinessRulesBridge();

        // Trigger definitions
        private Dictionary<String, Dictionary<String, List<Func<object, ExpandoObject>>>> m_triggerDefinitions = new Dictionary<string, Dictionary<string, List<Func<object, ExpandoObject>>>>();

        // Validators
        private Dictionary<String, List<Func<Object, Object[]>>> m_validatorDefinitions = new Dictionary<string, List<Func<object, Object[]>>>();

        /// <summary>
        /// Only one BRE can be created
        /// </summary>
        private JavascriptBusinessRulesEngine()
        {

        }

        /// <summary>
        /// Business rules bridge
        /// </summary>
        public BusinessRulesBridge Bridge {  get { return this.m_bridge; } }

        /// <summary>
        /// Initialize javascript BRE
        /// </summary>
        private void Initialize()
        {
            // Set up javascript ening 
            this.m_tracer.TraceInfo("OpenIZ Javascript Business Rules Host Initialize");
            
            this.m_engine = new Jint.Engine(cfg => cfg.AllowClr(
                    typeof(OpenIZ.Core.Model.BaseEntityData).GetTypeInfo().Assembly,
                    typeof(IBusinessRulesService<>).GetTypeInfo().Assembly
                ).Strict(false)
#if DEBUG
                .DebugMode(true)
#endif

                ).SetValue("OpenIZBre", this.m_bridge)
                .SetValue("console", new JsConsoleProvider());

            // Break
#if DEBUG
            this.m_engine.Break += M_engine_Break;
#endif 

            foreach(var itm in typeof(JavascriptBusinessRulesEngine).GetTypeInfo().Assembly.GetManifestResourceNames().Where(o=>o.EndsWith(".js")))
            using (StreamReader sr = new StreamReader(typeof(JavascriptBusinessRulesEngine).GetTypeInfo().Assembly.GetManifestResourceStream(itm)))
                this.AddRules(sr);
        }

        /// <summary>
        /// Break at the current line of execution
        /// </summary>
        private Jint.Runtime.Debugger.StepMode M_engine_Break(object sender, Jint.Runtime.Debugger.DebugInformation e)
        {

            if (System.Diagnostics.Debugger.IsAttached)
                System.Diagnostics.Debugger.Break();
            else
                return Jint.Runtime.Debugger.StepMode.None;

            // Modify this value on the VS debugger to control debugging
            var retVal = Jint.Runtime.Debugger.StepMode.None;
            return retVal;
        }

        /// <summary>
        /// Current BRE
        /// </summary>
        public static JavascriptBusinessRulesEngine Current
        {
            get
            {
                if (s_instance == null)
                    lock (s_syncLock)
                    {
                        s_instance = new JavascriptBusinessRulesEngine();
                        s_instance.Initialize();
                    }
                return s_instance;
            }
        }

        /// <summary>
        /// Gets the engine
        /// </summary>
        public Engine Engine { get { return this.m_engine; } }

        /// <summary>
        /// Add the specified script
        /// </summary>
        public void AddRules(StreamReader script)
        {
            try
            {
                this.m_tracer.TraceVerbose("Adding rules to BRE");
                var rawScript = script.ReadToEnd();
                // Find all reference paths
                Regex includeReg = new Regex(@"\/\/\/\s*?\<reference\s*?path\=[""'](.*?)[""']\s?\/\>", RegexOptions.Multiline);
                var incMatches = includeReg.Matches(rawScript);
                foreach(Match match in incMatches)
                {
                    var include = match.Groups[1].Value;
                    var incStream = (ApplicationServiceContext.Current.GetService(typeof(IDataReferenceResolver)) as IDataReferenceResolver)?.Resolve(include);
                    if (incStream == null)
                        this.m_tracer.TraceWarning("Include {0} not found", include);
                    else
                        using (StreamReader sr = new StreamReader(incStream))
                            this.m_engine.Execute(rawScript);
                }

            }
            catch(JavaScriptException ex)
            {
                this.m_tracer.TraceError("Error executing JavaScript {0}:{1} > {2}", ex.LineNumber, ex.Column, ex);
                throw ex;
            }
        }

        /// <summary>
        /// Register a validator which is responsible for validation
        /// </summary>
        public void RegisterValidator(string target, Func<object, Object[]> _delegate)
        {
            List<Func<object, Object[]>> validatorFunc = null;
            if (!this.m_validatorDefinitions.TryGetValue(target, out validatorFunc))
            {
                this.m_tracer.TraceInfo("Will try to create BRE service for {0}", target);
                // We need to create a rule service base and register it!!! :)
                // Find the target type
                var targetType = typeof(Act).GetTypeInfo().Assembly.ExportedTypes.FirstOrDefault(o => o.GetTypeInfo().GetCustomAttribute<JsonObjectAttribute>()?.Id == target);
                if (targetType == null)
                    throw new KeyNotFoundException(target);
                var ruleService = typeof(RuleServiceBase<>).MakeGenericType(targetType);
                var serviceManager = ApplicationServiceContext.Current.GetService(typeof(IServiceManager)) as IServiceManager;
                serviceManager.AddServiceProvider(ruleService);

                // Now add
                lock (s_syncLock)
                    this.m_validatorDefinitions.Add(target, new List<Func<object, Object[]>>() { _delegate });
            }
            else
                validatorFunc.Add(_delegate);

        }

        /// <summary>
        /// Register a rule
        /// </summary>
        public void RegisterRule(string target, string trigger, Func<object, ExpandoObject> _delegate)
        {
            Dictionary<String, List<Func<object, ExpandoObject>>> triggerHandler = null;
            if (!this.m_triggerDefinitions.TryGetValue(target, out triggerHandler))
            {
                this.m_tracer.TraceInfo("Will try to create BRE service for {0}", target);
                // We need to create a rule service base and register it!!! :)
                // Find the target type
                var targetType = typeof(Act).GetTypeInfo().Assembly.ExportedTypes.FirstOrDefault(o => o.GetTypeInfo().GetCustomAttribute<JsonObjectAttribute>()?.Id == target);
                if (targetType == null)
                    throw new KeyNotFoundException(target);
                var ruleService = typeof(RuleServiceBase<>).MakeGenericType(targetType);
                var serviceManager = ApplicationServiceContext.Current.GetService(typeof(IServiceManager)) as IServiceManager;
                serviceManager.AddServiceProvider(ruleService);

                // Now add
                lock (s_syncLock)
                    this.m_triggerDefinitions.Add(target, new Dictionary<string, List<Func<object, ExpandoObject>>>()
                    {
                        { trigger, new List<Func<object, ExpandoObject>>() { _delegate } }
                    });
            }
            else
            {
                List<Func<object, ExpandoObject>> delegates = null;
                if (!triggerHandler.TryGetValue(trigger, out delegates))
                    lock (s_syncLock)
                        triggerHandler.Add(trigger, new List<Func<object, ExpandoObject>>() { _delegate });
                else
                    delegates.Add(_delegate);
            }
        }

		/// <summary>
		/// Get call list of action
		/// </summary>
		/// <typeparam name="TBinding">The type of the t binding.</typeparam>
		/// <param name="action">The action.</param>
		/// <returns>List&lt;Func&lt;System.Object, ExpandoObject&gt;&gt;.</returns>
		public List<Func<object, ExpandoObject>> GetCallList<TBinding>(String action)
        {
            return this.GetCallList(typeof(TBinding), action);
        }

		/// <summary>
		/// Generic method for bindig call list
		/// </summary>
		/// <param name="tbinding">The tbinding.</param>
		/// <param name="action">The action.</param>
		/// <returns>List&lt;Func&lt;System.Object, ExpandoObject&gt;&gt;.</returns>
		public List<Func<object, ExpandoObject>> GetCallList(Type tbinding, String action) { 
            var className = tbinding.GetTypeInfo().GetCustomAttribute<JsonObjectAttribute>()?.Id;

            // Try to get the binding
            Dictionary<String, List<Func<object, ExpandoObject>>> triggerHandler = null;
            if (this.m_triggerDefinitions.TryGetValue(className, out triggerHandler))
            {
                List<Func<object, ExpandoObject>> callList = null;
                if (triggerHandler.TryGetValue(action, out callList))
                    return callList;
            }
            return new List<Func<object, ExpandoObject>>();

        }

        /// <summary>
        /// Get all validator functions
        /// </summary>
        public List<Func<Object, Object[]>> GetValidators<TBinding>()
        {
            var className = typeof(TBinding).GetTypeInfo().GetCustomAttribute<JsonObjectAttribute>()?.Id;
            
            // Try to get the binding
            List<Func<object, Object[]>> callList = null;
            if (this.m_validatorDefinitions.TryGetValue(className, out callList))
                return callList;
            return new List<Func<object, object[]>>();
        }

        /// <summary>
        /// Perform actual invokation on all objects
        /// </summary>
        public TBinding Invoke<TBinding>(string action, TBinding data) where TBinding : IdentifiedData
        {
            try
            {
                var callList = this.GetCallList<TBinding>(action);
                if(callList.Count == 0)
                    callList = this.GetCallList(data.GetType(), action);
                var retVal = data;
                foreach (var c in callList)
                {
                    dynamic viewModel = this.m_bridge.ToViewModel(retVal);
                    viewModel = c(viewModel);
                    retVal = (TBinding)this.m_bridge.ToModel(viewModel);
                }

                return retVal;
            }
            catch(Exception e)
            {
                this.m_tracer.TraceError("Error running {0} for {1} : {2}", action, data, e);
                return data;
            }
        }

        /// <summary>
        /// Validate the data object if validation is available
        /// </summary>
        public List<DetectedIssue> Validate<TBinding>(TBinding data) where TBinding : IdentifiedData
        {
            try
            {
                var callList = this.GetValidators<TBinding>();
                var retVal = new List<DetectedIssue>();
                foreach (var c in callList)
                {
                    var issues = c(this.m_bridge.ToViewModel(data));
                    retVal.AddRange(issues.Cast<IDictionary<String, Object>>().Select(o => new DetectedIssue()
                    {
                        Text = o.ContainsKey("text") ? o["text"]?.ToString() : null,
                        Priority = o.ContainsKey("priority") ? (DetectedIssuePriorityType)(int)(double)o["priority"] : DetectedIssuePriorityType.Informational
                    }));
                }
                return retVal;
            }
            catch(Exception e)
            {
                this.m_tracer.TraceError("Error validating {0} : {1}", data, e);
                return new List<DetectedIssue>()
                {
                    new DetectedIssue()
                    {
                        Priority = DetectedIssuePriorityType.Error,
                        Text = e.Message
                    }
                };
            }
        }
    }
}
