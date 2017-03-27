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
 * User: fyfej
 * Date: 2017-3-24
 */
using MARC.HI.EHRS.SVC.Core.Services;
using OpenIZ.Messaging.GS1.Wcf;
using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;

namespace OpenIZ.Messaging.GS1
{
	/// <summary>
	/// Stock service message handler
	/// </summary>
	public class StockServiceMessageHandler : IMessageHandlerService
	{
		// IMSI Trace host
		private readonly TraceSource traceSource = new TraceSource("OpenIZ.Messaging.GS1");

		// web host
		private WebServiceHost webHost;

		/// <summary>
		/// Fired when the object is starting up
		/// </summary>
		public event EventHandler Started;

		/// <summary>
		/// Fired when the object is starting
		/// </summary>
		public event EventHandler Starting;

		/// <summary>
		/// Fired when the service has stopped
		/// </summary>
		public event EventHandler Stopped;

		/// <summary>
		/// Fired when the service is stopping
		/// </summary>
		public event EventHandler Stopping;

		/// <summary>
		/// True if running
		/// </summary>
		public bool IsRunning => this.webHost?.State == CommunicationState.Opened;

		/// <summary>
		/// Start the service
		/// </summary>
		public bool Start()
		{
			try
			{
				this.Starting?.Invoke(this, EventArgs.Empty);

				this.webHost = new WebServiceHost(typeof(StockServiceBehavior));
				foreach (ServiceEndpoint endpoint in this.webHost.Description.Endpoints)
				{
					this.traceSource.TraceInformation("Starting GS1 on {0}...", endpoint.Address);
				}
				// Start the webhost
				this.webHost.Open();

				this.Started?.Invoke(this, EventArgs.Empty);
				return true;
			}
			catch (Exception e)
			{
				this.traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
				return false;
			}
		}

		/// <summary>
		/// Stop the IMSI service
		/// </summary>
		public bool Stop()
		{
			this.Stopping?.Invoke(this, EventArgs.Empty);

			if (this.webHost != null)
			{
				this.webHost.Close();
				this.webHost = null;
			}

			this.Stopped?.Invoke(this, EventArgs.Empty);

			return true;
		}
	}
}