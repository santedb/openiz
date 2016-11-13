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
 * Date: 2016-11-3
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Core.Event
{
	/// <summary>
	/// Represents a notification type.
	/// </summary>
	public enum NotificationType
	{
		/// <summary>
		/// Any action occurs. This is only used
		/// </summary>
		Any = Create | Update | DuplicatesResolved,

		/// <summary>
		/// Indicates a creation notification.
		/// </summary>
		Create = 0x1,
		
		/// <summary>
		/// Indicates a duplicates resolved notification.
		/// </summary>
		DuplicatesResolved = 0x2,

		/// <summary>
		/// Indicates a reconciliation required notification.
		/// </summary>
		ReconciliationRequired = 0x4,

		/// <summary>
		/// Indicates an update notification.
		/// </summary>
		Update = 0x8
	}
}
