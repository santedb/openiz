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
 * User: khannan
 * Date: 2017-1-5
 */

using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Data;
using MARC.HI.EHRS.SVC.Core.Services;
using OpenIZ.Core.Model.RISI;
using OpenIZ.Core.Security;
using OpenIZ.Reporting.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading.Tasks;
using System.Xml.Serialization;
using OpenIZ.Core.Diagnostics;
using OpenIZ.Reporting.Jasper.Model;

namespace OpenIZ.Reporting.Jasper
{
	/// <summary>
	/// Represents a Jasper server report handler.
	/// </summary>
	[Service(ServiceInstantiationType.Instance)]
	public class JasperReportHandler : IReportHandler, ISupportBasicAuthentication
	{
		/// <summary>
		/// The internal reference to the <see cref="HttpClient"/> instance.
		/// </summary>
		private readonly HttpClient client;

		/// <summary>
		/// The cookie container.
		/// </summary>
		private readonly CookieContainer cookieContainer;

		///// <summary>
		///// The internal reference to the <see cref="ParameterType"/> <see cref="IDataPersistenceService{TData}"/> instance.
		///// </summary>
		//private readonly IDataPersistenceService<ParameterType> parameterTypePersistenceService = ApplicationContext.Current.GetService<IDataPersistenceService<ParameterType>>();

		///// <summary>
		///// The internal reference to the <see cref="ReportDefinition"/> <see cref="IDataPersistenceService{TData}"/> instance.
		///// </summary>
		//private readonly IDataPersistenceService<ReportDefinition> reportDefinitionPersistenceService = ApplicationContext.Current.GetService<IDataPersistenceService<ReportDefinition>>();

		///// <summary>
		///// The internal reference to the <see cref="ReportFormat"/> <see cref="IDataPersistenceService{TData}"/> instance.
		///// </summary>
		//private readonly IDataPersistenceService<ReportFormat> reportFormatPersistenceService = ApplicationContext.Current.GetService<IDataPersistenceService<ReportFormat>>();

		///// <summary>
		///// The internal reference to the <see cref="ReportParameter"/> <see cref="IDataPersistenceService{TData}"/> instance.
		///// </summary>
		//private readonly IDataPersistenceService<ReportParameter> reportParameterPersistenceService = ApplicationContext.Current.GetService<IDataPersistenceService<ReportParameter>>();

		/// <summary>
		/// The jasper authentication path.
		/// </summary>
		private static readonly string JasperAuthPath = "/rest/login";

		/// <summary>
		/// The jasper report path.
		/// </summary>
		private static readonly string JasperReportPath = "/rest_v2/reports";

		/// <summary>
		/// The jasper resources path.
		/// </summary>
		private static readonly string JasperResourcesPath = "/rest_v2/resources";

		/// <summary>
		/// The tracer instance.
		/// </summary>
		private static readonly Tracer tracer = Tracer.GetTracer(typeof(JasperReportHandler));

		/// <summary>
		/// Initializes a new instance of the <see cref="JasperReportHandler"/> class.
		/// </summary>
		public JasperReportHandler()
		{
			this.cookieContainer = new CookieContainer();

			var handler = new HttpClientHandler
			{
				CookieContainer = this.cookieContainer
			};

			this.client = new HttpClient(handler);
		}

		/// <summary>
		/// Gets or sets the authentication result of the authentication handler.
		/// </summary>
		public AuthenticationResult AuthenticationResult { get; set; }

		/// <summary>
		/// Gets or sets the report URI.
		/// </summary>
		public Uri ReportUri { get; set; }

		/// <summary>
		/// Authenticates against a remote system using a username and password.
		/// </summary>
		/// <param name="username">The username of the user.</param>
		/// <param name="password">The password of the user.</param>
		public AuthenticationResult Authenticate(string username, string password)
		{
			var content = new StringContent($"j_username={username}&j_password={password}");

			// HACK: have to remove the headers before adding them...
			content.Headers.Remove("Content-Type");
			content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

			var response = this.client.PostAsync($"{this.ReportUri}{JasperAuthPath}", content).Result;

			if (response.IsSuccessStatusCode)
			{
				var values = response.Headers.GetValues("Set-Cookie").Where(c => c.StartsWith("JSESSIONID"));

				tracer.TraceEvent(EventLevel.Informational, "token: {0}", values);
			}
			else
			{
				throw new AuthenticationException($"Unable to authenticate against the Jasper Service, using username: {username}");
			}

			this.AuthenticationResult = new AuthenticationResult("87737BAAE3BE62B95136B653C65D8052");

			return this.AuthenticationResult;
		}

		/// <summary>
		/// Creates a new report parameter type.
		/// </summary>
		/// <param name="parameterType">The report parameter type to create.</param>
		/// <returns>Returns the created report parameter type.</returns>
		public ParameterType CreateParameterType(ParameterType parameterType)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates a new report definition.
		/// </summary>
		/// <param name="reportDefinition">The report definition to create.</param>
		/// <returns>Returns the created report definition.</returns>
		public ReportDefinition CreateReportDefinition(ReportDefinition reportDefinition)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates a report format.
		/// </summary>
		/// <param name="reportFormat">The report format to create.</param>
		/// <returns>Returns the created report format.</returns>
		public ReportFormat CreateReportFormat(ReportFormat reportFormat)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Deletes a report parameter type.
		/// </summary>
		/// <param name="id">The id of the report parameter type to delete.</param>
		/// <returns>Returns the deleted report parameter type.</returns>
		public ParameterType DeleteParameterType(Guid id)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Deletes a report definition.
		/// </summary>
		/// <param name="id">The id of the report definition to delete.</param>
		/// <returns>Returns the deleted report definition.</returns>
		public ReportDefinition DeleteReportDefinition(Guid id)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Deletes a report format.
		/// </summary>
		/// <param name="id">The id of the report format.</param>
		/// <returns>Returns the report deleted report format.</returns>
		public ReportFormat DeleteReportFormat(Guid id)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			this.client?.Dispose();
		}

		/// <summary>
		/// Gets a list of all report parameter types.
		/// </summary>
		/// <returns>Returns a list of report parameter types.</returns>
		public RisiCollection<ReportParameter> GetAllReportParamterTypes()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets a parameter type by id.
		/// </summary>
		/// <param name="id">The id of the parameter type to retrieve.</param>
		/// <returns>Returns a parameter type.</returns>
		public ParameterType GetParameterType(Guid id)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets a report definition by id.
		/// </summary>
		/// <param name="id">The id of the report definition to retrieve.</param>
		/// <returns>Returns a report definition.</returns>
		public ReportDefinition GetReportDefinition(Guid id)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets a list of report definitions based on a specific query.
		/// </summary>
		/// <returns>Returns a list of report definitions.</returns>
		public RisiCollection<ReportDefinition> GetReportDefintions()
		{
			var token = this.Authenticate("jasperadmin", "jasperadmin");

			cookieContainer.Add(new Cookie("JSESSIONID", token.ToString()) { Domain = "JSESSIONID" });
			var response = client.GetAsync($"{this.ReportUri}{JasperResourcesPath}").Result;

			tracer.TraceEvent(EventLevel.Informational, "Jasper report server response: {0}", response.Content.ToString());

			Resources resources = null;

			using (var stream = response.Content.ReadAsStreamAsync().Result)
			{
				XmlSerializer serializer = new XmlSerializer(typeof(Resources));

				resources = (Resources)serializer.Deserialize(stream);
			}

			var reports = new List<ReportDefinition>();

			foreach (var resourceLookup in resources.ResourceLookups)
			{
				switch (resourceLookup.ResourceType)
				{
					case "reportUnit":
						//var reportResponse = client.GetAsync($"{this.ReportUri}/{JasperResourcesPath}/{resourceLookup.Uri}").Result;

						var reportDefinition = new ReportDefinition(resourceLookup.Label)
						{
							CorrelationId = resourceLookup.Label,
							Description = resourceLookup.Description
						};

						reports.Add(reportDefinition);

						//this.reportDefinitionPersistenceService.Insert(reportDefinition, AuthenticationContext.Current.Principal, TransactionMode.Commit);
						break;
				}
			}

			//var reports = this.reportDefinitionPersistenceService.Query(r => r.Key != null, null);


			return new RisiCollection<ReportDefinition>(reports);
		}

		/// <summary>
		/// Gets a report format by id.
		/// </summary>
		/// <param name="id">The id of the report format to retrieve.</param>
		/// <returns>Returns a report format.</returns>
		public ReportFormat GetReportFormat(Guid id)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets a report parameter by id.
		/// </summary>
		/// <param name="id">The id of the report parameter to retrieve.</param>
		/// <returns>Returns a report parameter.</returns>
		public ReportParameter GetReportParameter(Guid id)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets a list of report parameters.
		/// </summary>
		/// <param name="id">The id of the report for which to retrieve parameters.</param>
		/// <returns>Returns a list of parameters.</returns>
		public RisiCollection<ReportParameter> GetReportParameters(Guid id)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets a list of auto-complete parameters which are applicable for the specified parameter.
		/// </summary>
		/// <param name="id">The id of the report.</param>
		/// <param name="parameterId">The id of the parameter for which to retrieve detailed information.</param>
		/// <returns>Returns an auto complete source definition of valid parameters values for a given parameter.</returns>
		public AutoCompleteSourceDefinition GetReportParameterValues(Guid id, Guid parameterId)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the report source.
		/// </summary>
		/// <param name="id">The id of the report for which to retrieve the source.</param>
		/// <returns>Returns the report source.</returns>
		public ReportDefinition GetReportSource(Guid id)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Runs a report.
		/// </summary>
		/// <param name="reportId">The id of the report.</param>
		/// <param name="reportFormat">The format of the report.</param>
		/// <param name="parameters">The parameters of the report.</param>
		public byte[] RunReport(Guid reportId, Guid reportFormat, IEnumerable<ReportParameter> parameters)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Updates a parameter type.
		/// </summary>
		/// <param name="parameterType">The updated parameter type.</param>
		/// <returns>Returns the updated parameter type.</returns>
		public ParameterType UpdateParameterType(ParameterType parameterType)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Updates a report definition.
		/// </summary>
		/// <param name="reportDefinition">The updated report definition.</param>
		/// <returns>Returns the updated report definition.</returns>
		public ReportDefinition UpdateReportDefinition(ReportDefinition reportDefinition)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Updates a report format.
		/// </summary>
		/// <param name="id">The id of the report format to update.</param>
		/// <param name="reportFormat">The updated report format.</param>
		/// <returns>Returns the update report format.</returns>
		public ReportFormat UpdateReportFormat(ReportFormat reportFormat)
		{
			throw new NotImplementedException();
		}
	}
}