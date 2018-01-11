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
 * User: justi
 * Date: 2016-11-30
 */
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace OpenIZ.Core.Applets.Model
{
    /// <summary>
    /// Represents a single view in the applet view state
    /// </summary>
    [XmlType(nameof(AppletView), Namespace = "http://openiz.org/applet")]
    public class AppletView : AppletViewState
    {
        
        /// <summary>
        /// Gets or sets the list of titles 
        /// </summary>
        [XmlElement("title")]
        public List<LocaleString> Title { get; set; }

        /// <summary>
        /// Gets or sets the controller for the object
        /// </summary>
        [XmlElement("controller")]
        public string Controller { get; set; }
        
    }
}