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
using System.Collections.Generic;

namespace OpenIZ.Core.Security.Tfa.Email.Configuration
{
	/// <summary>
	/// Represents the configuration for the TFA mecahnism
	/// </summary>
	public class MechanismConfiguration
	{
		/// <summary>
		/// Creates a new template mechanism configuration
		/// </summary>
		public MechanismConfiguration()
		{
			this.Templates = new List<TemplateConfiguration>();
		}

		/// <summary>
		/// SMTP configuration
		/// </summary>
		public SmtpConfiguration Smtp { get; set; }

		/// <summary>
		/// Template configuration
		/// </summary>
		public List<TemplateConfiguration> Templates { get; set; }
	}
}