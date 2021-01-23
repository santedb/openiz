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
 * User: khannan
 * Date: 2017-9-1
 */

using MARC.HI.EHRS.SVC.Core.Wcf;
using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace OpenIZ.Messaging.AMI.Wcf.Behavior
{
	/// <summary>
	/// Represents the endpoint behavior for the AMI endpoint.
	/// </summary>
	public class AmiRestEndpointBehavior : IEndpointBehavior
	{
		/// <summary>
		/// Adds the binding parameters.
		/// </summary>
		/// <param name="endpoint">The endpoint.</param>
		/// <param name="bindingParameters">The binding parameters.</param>
		public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
		{
		}

		/// <summary>
		/// Applies the client behavior.
		/// </summary>
		/// <param name="endpoint">The endpoint.</param>
		/// <param name="clientRuntime">The client runtime.</param>
		public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
		{
		}

		/// <summary>
		/// Applies dispatch behavior.
		/// </summary>
		/// <param name="endpoint">The endpoint for which to apply the behavior.</param>
		/// <param name="endpointDispatcher">The endpoint dispatcher of the endpoint.</param>
		public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
		{
			endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new AmiMessageInspector());
			endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new LogMessageInspector());

			// Apply to each operation the AMI formatter
			foreach (var op in endpoint.Contract.Operations)
			{
				op.OperationBehaviors.Add(new AmiSerializerOperationBehavior());
			}
		}

		/// <summary>
		/// Validates the specified endpoint.
		/// </summary>
		/// <param name="endpoint">The endpoint.</param>
		/// <exception cref="System.InvalidOperationException">AMI Must be bound to type webHttpBinding</exception>
		public void Validate(ServiceEndpoint endpoint)
		{
			var bindingElements = endpoint.Binding.CreateBindingElements();
			var webEncoder = bindingElements.Find<WebMessageEncodingBindingElement>();

			if (webEncoder == null)
			{
				throw new InvalidOperationException("AMI Must be bound to type webHttpBinding");
			}
		}
	}
}