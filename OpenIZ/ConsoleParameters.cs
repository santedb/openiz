﻿/*
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
using MohawkCollege.Util.Console.Parameters;
using System.ComponentModel;

namespace OpenIZ
{
    /// <summary>
    /// Console parameters for startup
    /// </summary>
    public class ConsoleParameters
    {

        /// <summary>
        /// When true, parameters should be shown
        /// </summary>
        [Description("Shows help and exits")]
        [Parameter("?")]
        [Parameter("help")]
        public bool ShowHelp { get; set; }

        /// <summary>
        /// When true console mode should be enabled
        /// </summary>
        [Description("Instructs the host process to run in console mode")]
        [Parameter("c")]
        [Parameter("console")]
        public bool ConsoleMode { get; set; }

        /// <summary>
        /// Start test
        /// </summary>
        [Description("Instructs the host process to perform startup functions and then shut down immediately")]
        [Parameter("test-start")]
        public bool StartupTest { get; set; }

        /// <summary>
        /// Install the service
        /// </summary>
        [Parameter("install")]
        [Description("Register the service in Windows")]
        public bool Install { get; set; }

        /// <summary>
        /// Uninstall the service
        /// </summary>
        [Parameter("uninstall")]
        [Description("Unregister the service in Windows")]
        public bool UnInstall { get; set; }

        /// <summary>
        /// Gets or sets the instance name
        /// </summary>
        [Parameter("name")]
        [Description("Sets the instance name")]
        public string InstanceName { get; set; }
    }
}