﻿/*
 * Copyright 2016-2016 Mohawk College of Applied Arts and Technology
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
 * Date: 2016-1-19
 */
using System;
using System.Runtime.Serialization;

namespace OpenIZ.Core.Model.DataTypes
{
    /// <summary>
    /// Identifies a classification for a concept
    /// </summary>
    [Serializable]
    [DataContract(Name = "ConceptClass", Namespace = "http://openiz.org/model")]
    public class ConceptClass : IdentifiedData
    {

        /// <summary>
        /// Gets or sets the name of the concept class
        /// </summary>
        [DataMember(Name = "name")]
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the mnemonic
        /// </summary>
        [DataMember(Name = "mnemonic")]
        public string Mnemonic { get; set; }


    }
}