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

using MARC.HI.EHRS.SVC.Messaging.HAPI;
using MARC.HI.EHRS.SVC.Messaging.HAPI.TransportProtocol;
using NHapi.Base.Model;

namespace OpenIZ.Messaging.HL7
{
	/// <summary>
	/// Represents a message handler for unsupported messages.
	/// </summary>
	public class NotSupportedMessageHandler : IHL7MessageHandler
	{
		/// <summary>
		/// Handles unsupported messages.
		/// </summary>
		/// <param name="e">The received message event arguments.</param>
		/// <returns>Returns a message.</returns>
		public IMessage HandleMessage(Hl7MessageReceivedEventArgs e)
		{
			return MessageUtil.CreateNack(e.Message, "AR", "200", "Unsupported message type");
		}
	}
}