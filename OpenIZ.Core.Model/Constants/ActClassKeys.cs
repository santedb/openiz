﻿/*
 * Copyright 2015-2016 Mohawk College of Applied Arts and Technology
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
 * Date: 2016-6-14
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Core.Model.Constants
{
    
    /// <summary>
    /// Represents a series of class keys for use on acts
    /// </summary>
    public static class ActClassKeys
    {
        /// <summary>
        /// The act represents a substance administration
        /// </summary>
        public static readonly Guid SubstanceAdministration = Guid.Parse("932A3C7E-AD77-450A-8A1F-030FC2855450");
        /// <summary>
        /// The act represetns a condition
        /// </summary>
        public static readonly Guid Condition = Guid.Parse("1987C53C-7AB8-4461-9EBC-0D428744A8C0");
        /// <summary>
        /// The act represents a registration
        /// </summary>
        public static readonly Guid Registration = Guid.Parse("6BE8D358-F591-4A3A-9A57-1889B0147C7E");
        /// <summary>
        /// The act represents a procedure
        /// </summary>
        public static readonly Guid Observation = Guid.Parse("28D022C6-8A8B-47C4-9E6A-2BC67308739E");
        /// <summary>
        /// The act represents an informational act
        /// </summary>
        public static readonly Guid Inform = Guid.Parse("192F1768-D39E-409D-87BE-5AFD0EE0D1FE");
        /// <summary>
        /// The act represents an encounter
        /// </summary>
        public static readonly Guid Encounter = Guid.Parse("54B52119-1709-4098-8911-5DF6D6C84140");
        /// <summary>
        /// The act represents a simple battery of procedures/administrations/etc
        /// </summary>
        public static readonly Guid Battery = Guid.Parse("676DE278-64AA-44F2-9B69-60D61FC1F5F5");
        /// <summary>
        /// Generic act
        /// </summary>
        public static readonly Guid Act = Guid.Parse("D874424E-C692-4FD8-B94E-642E1CBF83E9");
        /// <summary>
        /// The act represents a procedure (something done to a patient)
        /// </summary>
        public static readonly Guid Procedure = Guid.Parse("8CC5EF0D-3911-4D99-937F-6CFDC2A27D55");
        /// <summary>
        /// The act represents some provision of care
        /// </summary>
        public static readonly Guid CareProvision = Guid.Parse("1071D24E-6FE9-480F-8A20-B1825AE4D707");
        /// <summary>
        /// The act represents generic account management
        /// </summary>
        public static readonly Guid AccountManagement = Guid.Parse("CA44A469-81D7-4484-9189-CA1D55AFECBC");
        /// <summary>
        /// The act represents a supply of some material
        /// </summary>
        public static readonly Guid Supply = Guid.Parse("A064984F-9847-4480-8BEA-DDDF64B3C77C");
        /// <summary>
        /// Control act event
        /// </summary>
        public static readonly Guid ControlAct = Guid.Parse("B35488CE-B7CD-4DD4-B4DE-5F83DC55AF9F");
        /// <summary>
        /// The physical transporting of materials or people from one place to another
        /// </summary>
        public static readonly Guid Transport = Guid.Parse("61677F76-DC05-466D-91DE-47EFC8E7A3E6");
    }
}