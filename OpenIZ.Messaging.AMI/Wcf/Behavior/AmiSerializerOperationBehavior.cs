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

using OpenIZ.Core.Wcf.Serialization;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace OpenIZ.Messaging.AMI.Wcf.Behavior
{
	/// <summary>
	/// AMI Serializer operation behavior
	/// </summary>
	internal class AmiSerializerOperationBehavior : IOperationBehavior
	{
		/// <summary>
		/// Adds the binding parameters.
		/// </summary>
		/// <param name="operationDescription">The operation description.</param>
		/// <param name="bindingParameters">The binding parameters.</param>
		public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters)
		{
		}

		/// <summary>
		/// Applies the client behavior.
		/// </summary>
		/// <param name="operationDescription">The operation description.</param>
		/// <param name="clientOperation">The client operation.</param>
		public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
		{
		}

		/// <summary>
		/// Apply the dispatch behavior.
		/// </summary>
		/// <param name="operationDescription">The operation description.</param>
		/// <param name="dispatchOperation">The dispatch description.</param>
		public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
		{
			dispatchOperation.Formatter = new WcfMessageDispatchFormatter<IAmiContract>(operationDescription);
		}

		/// <summary>
		/// Validates the specified operation description.
		/// </summary>
		/// <param name="operationDescription">The operation description.</param>
		public void Validate(OperationDescription operationDescription)
		{
		}
	}
}