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
using OpenIZ.Core.Applets.Services;
using OpenIZ.Core.Diagnostics;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OpenIZ.BusinessRules.JavaScript
{
    /// <summary>
    /// Represents a utility class which loads business rules from the applet manager
    /// </summary>
    public class AppletBusinessRuleLoader
    {
        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(AppletBusinessRuleLoader));

        /// <summary>
        /// Load all rules from applets
        /// </summary>
        public void LoadRules(Assembly serializerAssembly = null)
        {
            try
            {
                var solutionManager = ApplicationServiceContext.Current.GetService(typeof(IAppletManagerService)) as IAppletManagerService;
                foreach (var itm in solutionManager.Applets.SelectMany(c => c.Assets).Where(a => a.Name.StartsWith("rules/")))
                    using (StreamReader sr = new StreamReader(new MemoryStream(solutionManager.Applets.RenderAssetContent(itm))))
                    {
                        var script = sr.ReadToEnd();
                        JavascriptExecutorPool.Current.ExecuteGlobal(o => o.ExecuteScript(itm.ToString(), script));
                        this.m_tracer.TraceInfo("Added rules from {0}", itm.Name);
                    }

                //// Instruct the rules engine to load rules
                //OpenIZ.BusinessRules.JavaScript.JavascriptBusinessRulesEngine.EngineCreated += (o, e) =>
                //{
                //    foreach (var itm in appletManager.Applets.SelectMany(a => a.Assets).Where(a => a.Name.StartsWith("rules/")))
                //        using (StreamReader sr = new StreamReader(new MemoryStream(appletManager.Applets.RenderAssetContent(itm))))
                //        {
                //            e.CreatedEngine.AddRules(itm.Name, sr);
                //            this.m_tracer.TraceInfo("Added rules from {0}", itm.Name);
                //        }
                //};
            }
            catch (Exception ex)
            {
                this.m_tracer.TraceError("Error on startup: {0}", ex);
                throw new InvalidOperationException("Could not start business rules engine manager service", ex);
            }
        }
    }
}
