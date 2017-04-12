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
 * User: Nityan
 * Date: 2017-4-4
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Reporting.Core.Attributes
{
	/// <summary>
	/// Represents an attribute which restrict access to reporting functions.
	/// </summary>
	/// <seealso cref="System.Security.Permissions.CodeAccessSecurityAttribute" />
	[AttributeUsage(AttributeTargets.Method)]
	public class ReportExecutorAuthorizeAttribute : CodeAccessSecurityAttribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ReportExecutorAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="action">One of the <see cref="T:System.Security.Permissions.SecurityAction" /> values.</param>
		public ReportExecutorAuthorizeAttribute(SecurityAction action) : base(action)
		{
		}

		/// <summary>
		/// When overridden in a derived class, creates a permission object that can then be serialized into binary form and persistently stored along with the <see cref="T:System.Security.Permissions.SecurityAction" /> in an assembly's metadata.
		/// </summary>
		/// <returns>A serializable permission object.</returns>
		public override IPermission CreatePermission()
		{
			throw new NotImplementedException();
		}
	}
}
