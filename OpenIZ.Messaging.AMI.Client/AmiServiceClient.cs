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
 * Date: 2016-8-2
 */
using OpenIZ.Core.Alert.Alerting;
using OpenIZ.Core.Http;
using OpenIZ.Core.Interop.Clients;
using OpenIZ.Core.Model.AMI.Alerting;
using OpenIZ.Core.Model.AMI.Auth;
using OpenIZ.Core.Model.AMI.DataTypes;
using OpenIZ.Core.Model.AMI.Diagnostics;
using OpenIZ.Core.Model.AMI.Security;
using OpenIZ.Core.Model.DataTypes;
using OpenIZ.Core.Model.Entities;
using OpenIZ.Core.Model.Query;
using OpenIZ.Core.Model.Security;
using System;
using System.Linq;
using System.Linq.Expressions;
using OpenIZ.Core.Model.AMI.Applet;

namespace OpenIZ.Messaging.AMI.Client
{
	/// <summary>
	/// Represents the AMI service client.
	/// </summary>
	public class AmiServiceClient : ServiceClientBase, IDisposable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AmiServiceClient"/> class
		/// with a specified <see cref="IRestClient"/> instance.
		/// </summary>
		/// <param name="client">The <see cref="IRestClient"/> instance.</param>
		public AmiServiceClient(IRestClient client) : base(client)
		{
            this.Client.Accept = client.Accept ?? "application/xml";
        }

        /// <summary>
        /// Submits a diagnostics report
        /// </summary>
        public DiagnosticReport SubmitDiagnosticReport(DiagnosticReport report)
        {
            return this.Client.Post<DiagnosticReport, DiagnosticReport>(string.Format("sherlock"), this.Client.Accept, report);
        }

		/// <summary>
		/// Accepts a certificate signing request.
		/// </summary>
		/// <param name="id">The id of the certificate signing request.</param>
		/// <returns>Returns the submission result.</returns>
		public SubmissionResult AcceptCertificateSigningRequest(string id)
		{
			return this.Client.Put<object, SubmissionResult>(string.Format("csr/accept/{0}", id), this.Client.Accept, null);
		}

		/// <summary>
		/// Changes the password of a user.
		/// </summary>
		/// <param name="id">The id of the user whose password is to be changed.</param>
		/// <param name="password">The new password of the user.</param>
		/// <returns>Returns the updated user.</returns>
		[Obsolete("Should update the user with new password instead")]
		public SecurityUser ChangePassword(Guid id, string password)
		{
			return this.Client.Put<string, SecurityUser>(string.Format("changepassword/{0}", id.ToString()), this.Client.Accept, password);
		}

		/// <summary>
		/// Creates an alert message.
		/// </summary>
		/// <param name="alertMessageInfo">The alert message to be created.</param>
		/// <returns>Returns the created alert message.</returns>
		public AlertMessageInfo CreateAlert(AlertMessageInfo alertMessageInfo)
		{
			return this.Client.Post<AlertMessageInfo, AlertMessageInfo>("alert", this.Client.Accept, alertMessageInfo);
		}

		/// <summary>
		/// Creates an applet.
		/// </summary>
		/// <param name="appletManifestInfo">The applet manifest info to be created.</param>
		/// <returns>Returns the created applet manifest info.</returns>
		public AppletManifestInfo CreateApplet(AppletManifestInfo appletManifestInfo)
		{
			return this.Client.Post<AppletManifestInfo, AppletManifestInfo>("applet", this.Client.Accept, appletManifestInfo);
		}

		/// <summary>
		/// Creates a security application.
		/// </summary>
		/// <param name="applicationInfo">The security application to be created.</param>
		/// <returns>Returns the created security application.</returns>
		public SecurityApplicationInfo CreateApplication(SecurityApplicationInfo applicationInfo)
		{
			return this.Client.Post<SecurityApplicationInfo, SecurityApplicationInfo>("application", this.Client.Accept, applicationInfo);
		}

        /// <summary>
        /// Creates an assigning authority.
        /// </summary>
        /// <param name="assigningAuthorityInfo">The assigning authority to be created.</param>
        /// <returns>Returns the created assigning authority.</returns>
        public AssigningAuthorityInfo CreateAssigningAuthority(AssigningAuthorityInfo assigningAuthorityInfo)
        {
            return this.Client.Post<AssigningAuthorityInfo, AssigningAuthorityInfo>("assigningAuthority", this.Client.Accept, assigningAuthorityInfo);
        }

        /// <summary>
        /// Creates a device in the IMS.
        /// </summary>
        /// <param name="device">The device to be created.</param>
        /// <returns>Returns the newly created device.</returns>
        public SecurityDevice CreateDevice(SecurityDevice device)
		{
			return this.Client.Post<SecurityDevice, SecurityDevice>("device", this.Client.Accept, device);
		}

		/// <summary>
		/// Creates a place in the IMS.
		/// </summary>
		/// <param name="place">The place to be created.</param>
		/// <returns>Returns the newly created place.</returns>
		public Place CreatePlace(Place place)
		{
			return this.Client.Post<Place, Place>("place", this.Client.Accept, place);
		}

		/// <summary>
		/// Creates a policy in the IMS.
		/// </summary>
		/// <param name="policy">The policy to be created.</param>
		/// <returns>Returns the newly created policy.</returns>
		public SecurityPolicyInfo CreatePolicy(SecurityPolicyInfo policy)
		{
			return this.Client.Post<SecurityPolicyInfo, SecurityPolicyInfo>("policy", this.Client.Accept, policy);
		}

		/// <summary>
		/// Creates a role in the IMS.
		/// </summary>
		/// <param name="role">The role to be created.</param>
		/// <returns>Returns the newly created role.</returns>
		public SecurityRoleInfo CreateRole(SecurityRoleInfo role)
		{
			return this.Client.Post<SecurityRoleInfo, SecurityRoleInfo>("role", this.Client.Accept, role);
		}

		/// <summary>
		/// Creates a user in the IMS.
		/// </summary>
		/// <param name="user">The user to be created.</param>
		/// <returns>Returns the newly created user.</returns>
		public SecurityUserInfo CreateUser(SecurityUserInfo user)
		{
			return this.Client.Post<SecurityUserInfo, SecurityUserInfo>("user", this.Client.Accept, user);
		}

		/// <summary>
		/// Deletes an applet.
		/// </summary>
		/// <param name="appletId">The id of the applet to be deleted.</param>
		/// <returns>Returns the deleted applet.</returns>
		public AppletManifestInfo DeleteApplet(string appletId)
		{
			return this.Client.Delete<AppletManifestInfo>(string.Format("applet/{0}", appletId));
		}

		/// <summary>
		/// Deletes an application.
		/// </summary>
		/// <param name="applicationId">The id of the application to be deleted.</param>
		/// <returns>Returns the deleted application.</returns>
		public SecurityApplicationInfo DeleteApplication(string applicationId)
		{
			return this.Client.Delete<SecurityApplicationInfo>(string.Format("application/{0}", applicationId));
		}

		/// <summary>
		/// Deletes an assigning authority.
		/// </summary>
		/// <param name="assigningAuthorityId">The id of the assigning authority to be deleted.</param>
		/// <returns>Returns the deleted assigning authority.</returns>
		public AssigningAuthorityInfo DeleteAssigningAuthority(string assigningAuthorityId)
		{
			return this.Client.Delete<AssigningAuthorityInfo>(string.Format("assigningAuthority/{0}", assigningAuthorityId));
		}

		/// <summary>
		/// Deletes a device.
		/// </summary>
		/// <param name="id">The id of the device to be deleted.</param>
		/// <returns>Returns the deleted device.</returns>
		public SecurityDevice DeleteDevice(string id)
		{
			return this.Client.Delete<SecurityDevice>(string.Format("device/{0}", id));
		}

		/// <summary>
		/// Deletes a place.
		/// </summary>
		/// <param name="id">The id of the place to be deleted.</param>
		/// <returns>Returns the deleted place.</returns>
		public Place DeletePlace(string id)
		{
			return this.Client.Delete<Place>(string.Format("place/{0}", id));
		}

		/// <summary>
		/// Deletes a security policy.
		/// </summary>
		/// <param name="id">The id of the policy to be deleted.</param>
		/// <returns>Returns the deleted policy.</returns>
		public SecurityPolicyInfo DeletePolicy(string id)
		{
			return this.Client.Delete<SecurityPolicyInfo>(string.Format("policy/{0}", id));
		}

		/// <summary>
		/// Deletes a security role.
		/// </summary>
		/// <param name="id">The id of the role to be deleted.</param>
		/// <returns>Returns the deleted role.</returns>
		public SecurityRoleInfo DeleteRole(string id)
		{
			return this.Client.Delete<SecurityRoleInfo>(string.Format("role/{0}", id));
		}

		/// <summary>
		/// Deletes a security user.
		/// </summary>
		/// <param name="id">The id of the user to be deleted.</param>
		/// <returns>Returns the deleted user.</returns>
		public SecurityUserInfo DeleteUser(string id)
		{
			return this.Client.Delete<SecurityUserInfo>(string.Format("user/{0}", id));
		}

		#region IDisposable Support

		private bool disposedValue = false; // To detect redundant calls

		/// <summary>
		/// Dispose of any resources.
		/// </summary>
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose of any managed resources.
		/// </summary>
		/// <param name="disposing">Whether the current invocation is disposing.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					this.Client?.Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~AmiServiceClient() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		#endregion IDisposable Support

		/// <summary>
		/// Retrieves the specified role from the AMI
		/// </summary>
		public AmiCollection<SecurityPolicyInfo> FindPolicy(Expression<Func<SecurityPolicy, bool>> query)
		{
			var queryParms = QueryExpressionBuilder.BuildQuery(query).ToArray();
			return this.Client.Get<AmiCollection<SecurityPolicyInfo>>("policy", queryParms);
		}

		/// <summary>
		/// Retrieves the specified role from the AMI
		/// </summary>
		public AmiCollection<SecurityRoleInfo> FindRole(Expression<Func<SecurityRole, bool>> query)
		{
			var queryParms = QueryExpressionBuilder.BuildQuery(query).ToArray();
			return this.Client.Get<AmiCollection<SecurityRoleInfo>>("role", queryParms);
		}

		/// <summary>
		/// Gets a list of alerts.
		/// </summary>
		/// <param name="query">The query expression to use to find the alerts.</param>
		/// <returns>Returns a collection of alerts which match the specified criteria.</returns>
		public AmiCollection<AlertMessageInfo> GetAlerts(Expression<Func<AlertMessage, bool>> query)
		{
			return this.Client.Get<AmiCollection<AlertMessageInfo>>("alert", QueryExpressionBuilder.BuildQuery(query).ToArray());
		}

		/// <summary>
		/// Gets a list of applets for a specific query.
		/// </summary>
		/// <returns>Returns a list of applet which match the specific query.</returns>
		public AmiCollection<AppletManifestInfo> GetApplets(Expression<Func<AppletManifestInfo, bool>> query)
		{
			return this.Client.Get<AmiCollection<AppletManifestInfo>>("applet", QueryExpressionBuilder.BuildQuery(query).ToArray());
		}

		/// <summary>
		/// Gets a list applications for a specific query.
		/// </summary>
		/// <returns>Returns a list of application which match the specific query.</returns>
		public AmiCollection<SecurityApplicationInfo> GetApplications(Expression<Func<SecurityApplicationInfo, bool>> query)
		{
			return this.Client.Get<AmiCollection<SecurityApplicationInfo>>("application", QueryExpressionBuilder.BuildQuery(query).ToArray());
		}

		/// <summary>
		/// Gets a list of assigning authorities.
		/// </summary>
		/// <param name="query">The query expression to use to find the assigning authorities.</param>
		/// <returns>Returns a collection of assigning authorities which match the specified criteria.</returns>
		public AmiCollection<AssigningAuthorityInfo> GetAssigningAuthorities(Expression<Func<AssigningAuthority, bool>> query)
		{
			return this.Client.Get<AmiCollection<AssigningAuthorityInfo>>("assigningAuthority", QueryExpressionBuilder.BuildQuery(query).ToArray());
		}

		/// <summary>
		/// Gets a list of certificates.
		/// </summary>
		/// <param name="query">The query expression to use to find the certificates.</param>
		/// <returns>Returns a collection of certificates which match the specified query.</returns>
		public AmiCollection<X509Certificate2Info> GetCertificates(Expression<Func<X509Certificate2Info, bool>> query)
		{
			return this.Client.Get<AmiCollection<X509Certificate2Info>>("certificate", QueryExpressionBuilder.BuildQuery(query).ToArray());
		}

		/// <summary>
		/// Gets a certificate signing request.
		/// </summary>
		/// <param name="id">The id of the certificate signing request to be retrieved.</param>
		/// <returns>Returns a certificate signing request.</returns>
		public SubmissionResult GetCertificateSigningRequest(string id)
		{
			return this.Client.Get<SubmissionResult>(string.Format("csr/{0}", id));
		}

		/// <summary>
		/// Gets a list of certificate signing requests.
		/// </summary>
		/// <param name="query">The query expression to use to find the certificate signing requests.</param>
		/// <returns>Returns a collection of certificate signing requests which match the specified query.</returns>
		public AmiCollection<SubmissionInfo> GetCertificateSigningRequests(Expression<Func<SubmissionInfo, bool>> query)
		{
			return this.Client.Get<AmiCollection<SubmissionInfo>>("csr", QueryExpressionBuilder.BuildQuery(query).ToArray());
		}

		/// <summary>
		/// Gets a list of concepts.
		/// </summary>
		/// <param name="query">The query expression to use to find the concepts.</param>
		/// <returns>Returns a collection of concepts which match the specified query.</returns>
		public AmiCollection<Concept> GetConcepts(Expression<Func<Concept, bool>> query)
		{
			return this.Client.Get<AmiCollection<Concept>>("concept", QueryExpressionBuilder.BuildQuery(query).ToArray());
		}

		/// <summary>
		/// Gets a list of concept sets.
		/// </summary>
		/// <param name="query">The query expression to use to find the concept sets.</param>
		/// <returns>Returns a collection of concept sets which match the specified query.</returns>
		public AmiCollection<ConceptSet> GetConceptSets(Expression<Func<ConceptSet, bool>> query)
		{
			return this.Client.Get<AmiCollection<ConceptSet>>("conceptset", QueryExpressionBuilder.BuildQuery(query).ToArray());
		}

		/// <summary>
		/// Gets a list of devices.
		/// </summary>
		/// <param name="query">The query expression to use to find the devices.</param>
		/// <returns>Returns a collection of devices which match the specified query.</returns>
		public AmiCollection<SecurityDevice> GetDevices(Expression<Func<SecurityDevice, bool>> query)
		{
			return this.Client.Get<AmiCollection<SecurityDevice>>("device", QueryExpressionBuilder.BuildQuery(query).ToArray());
		}

		/// <summary>
		/// Gets a list of places.
		/// </summary>
		/// <param name="query">The query expression to use to find the places.</param>
		/// <returns>Returns a collection of places which match the specified query.</returns>
		public AmiCollection<Place> GetPlaces(Expression<Func<Place, bool>> query)
		{
			return this.Client.Get<AmiCollection<Place>>("place", QueryExpressionBuilder.BuildQuery(query).ToArray());
		}

		/// <summary>
		/// Retrieves a specified policy.
		/// </summary>
		/// <param name="query">The query expression to use to find the policy.</param>
		/// <returns>Returns a collection of policies which match the specified query parameters.</returns>
		public AmiCollection<SecurityPolicyInfo> GetPolicies(Expression<Func<SecurityPolicy, bool>> query)
		{
			var queryParms = QueryExpressionBuilder.BuildQuery(query).ToArray();
			return this.Client.Get<AmiCollection<SecurityPolicyInfo>>("policy", queryParms);
		}

		/// <summary>
		/// Gets a specific policy.
		/// </summary>
		/// <param name="id">The id of the policy to be retrieved.</param>
		/// <returns>Returns the specific policy.</returns>
		public SecurityPolicyInfo GetPolicy(string id)
		{
			return this.Client.Get<SecurityPolicyInfo>(string.Format("policy/{0}", id));
		}

		/// <summary>
		/// Gets a specific role.
		/// </summary>
		/// <param name="id">The id of the role to be retrieved.</param>
		/// <returns>Returns the specified user.</returns>
		public SecurityRoleInfo GetRole(string id)
		{
			return this.Client.Get<SecurityRoleInfo>(string.Format("role/{0}", id));
		}

		/// <summary>
		/// Retrieves a specified role.
		/// </summary>
		/// <param name="query">The query expression to use to find the role.</param>
		/// <returns>Returns a collection of roles which match the specified query parameters.</returns>
		public AmiCollection<SecurityRoleInfo> GetRoles(Expression<Func<SecurityRole, bool>> query)
		{
			var queryParms = QueryExpressionBuilder.BuildQuery(query).ToArray();
			return this.Client.Get<AmiCollection<SecurityRoleInfo>>("role", queryParms);
		}

		/// <summary>
		/// Gets a specific user.
		/// </summary>
		/// <param name="id">The id of the user to be retrieved.</param>
		/// <returns>Returns the specified user.</returns>
		public SecurityUserInfo GetUser(string id)
		{
			return this.Client.Get<SecurityUserInfo>(string.Format("user/{0}", id));
		}

		/// <summary>
		/// Retrieves a specified user.
		/// </summary>
		/// <param name="query">The query expression to use to find the user.</param>
		/// <returns>Returns a collection of users which match the specified query parameters.</returns>
		public AmiCollection<SecurityUserInfo> GetUsers(Expression<Func<SecurityUser, bool>> query)
		{
			var queryParms = QueryExpressionBuilder.BuildQuery(query).ToArray();
			return this.Client.Get<AmiCollection<SecurityUserInfo>>("user", queryParms);
		}

		/// <summary>
		/// Submits a certificate signing request to the AMI.
		/// </summary>
		/// <param name="submissionRequest">The certificate signing request.</param>
		/// <returns>Returns the submission result.</returns>
		public SubmissionResult SubmitCertificateSigningRequest(SubmissionRequest submissionRequest)
		{
			return this.Client.Post<SubmissionRequest, SubmissionResult>("csr", this.Client.Accept, submissionRequest);
		}

		/// <summary>
		/// Updates an alert.
		/// </summary>
		/// <param name="alertId">The id of the alert to be updated.</param>
		/// <param name="alertMessageInfo">The alert message info containing the updated information.</param>
		/// <returns>Returns the updated alert.</returns>
		public AlertMessageInfo UpdateAlert(string alertId, AlertMessageInfo alertMessageInfo)
		{
			return this.Client.Put<AlertMessageInfo, AlertMessageInfo>(string.Format("alert/{0}", alertId), this.Client.Accept, alertMessageInfo);
		}

		/// <summary>
		/// Updates an applet.
		/// </summary>
		/// <param name="appletId">The id of the applet to be updated.</param>
		/// <param name="appletManifestInfo">The applet containing the updated information.</param>
		/// <returns>Returns the updated applet.</returns>
		public AppletManifestInfo UpdateApplet(string appletId, AppletManifestInfo appletManifestInfo)
		{
			return this.Client.Put<AppletManifestInfo, AppletManifestInfo>(string.Format("applet/{0}", appletId), this.Client.Accept, appletManifestInfo);
		}

		/// <summary>
		/// Updates an application.
		/// </summary>
		/// <param name="applicationId">The id of the application to be updated.</param>
		/// <param name="applicationInfo">The application containing the updated information.</param>
		/// <returns>Returns the updated application.</returns>
		public SecurityApplicationInfo UpdateApplication(string applicationId, SecurityApplicationInfo applicationInfo)
		{
			return this.Client.Put<SecurityApplicationInfo, SecurityApplicationInfo>(string.Format("application/{0}", applicationId), this.Client.Accept, applicationInfo);
		}

		/// <summary>
		/// Updates an assigning authority.
		/// </summary>
		/// <param name="assigningAuthorityId">The id of the assigning authority to be updated.</param>
		/// <param name="assigningAuthorityInfo">The assigning authority info containing the updated information.</param>
		/// <returns>Returns the updated assigning authority.</returns>
		public AssigningAuthorityInfo UpdateAssigningAuthority(string assigningAuthorityId, AssigningAuthorityInfo assigningAuthorityInfo)
        {
            return this.Client.Put<AssigningAuthorityInfo, AssigningAuthorityInfo>(string.Format("assigningAuthority/{0}", assigningAuthorityId), this.Client.Accept, assigningAuthorityInfo);
        }

        /// <summary>
        /// Updates a concept.
        /// </summary>
        /// <param name="conceptId">The id of the concept to be updated.</param>
        /// <param name="concept">The concept containing the updated information.</param>
        /// <returns>Returns the updated concept.</returns>
        public Concept UpdateConcept(string conceptId, Concept concept)
		{
			return this.Client.Put<Concept, Concept>(string.Format("concept/{0}", conceptId), this.Client.Accept, concept);
		}

		/// <summary>
		/// Updates a place.
		/// </summary>
		/// <param name="placeId">The id of the place to be updated.</param>
		/// <param name="place">The place containing the updated information.</param>
		/// <returns>Returns the updated place.</returns>
		public Place UpdatePlace(string placeId, Place place)
		{
			return this.Client.Put<Place, Place>(string.Format("place/{0}", placeId), this.Client.Accept, place);
		}

		/// <summary>
		/// Updates a policy.
		/// </summary>
		/// <param name="policyId">The id of the policy to be updated.</param>
		/// <param name="policyInfo">The policy containing the updated information.</param>
		/// <returns>Returns the updated policy.</returns>
		public SecurityPolicyInfo UpdatePolicy(string policyId, SecurityPolicyInfo policyInfo)
		{
			return this.Client.Put<SecurityPolicyInfo, SecurityPolicyInfo>(string.Format("policy/{0}", policyId), this.Client.Accept, policyInfo);
		}

		/// <summary>
		/// Updates a role.
		/// </summary>
		/// <param name="roleId">The id of the role to be updated.</param>
		/// <param name="roleInfo">The role containing the updated information.</param>
		/// <returns>Returns the updated role.</returns>
		public SecurityRoleInfo UpdateRole(string roleId, SecurityRoleInfo roleInfo)
		{
			return this.Client.Put<SecurityRoleInfo, SecurityRoleInfo>(string.Format("role/{0}", roleId), this.Client.Accept, roleInfo);
		}

		/// <summary>
		/// Updates a user.
		/// </summary>
		/// <param name="id">The id of the user to be updated.</param>
		/// <param name="user">The user containing the updated information.</param>
		/// <returns>Returns the updated user.</returns>
		public SecurityUserInfo UpdateUser(Guid id, SecurityUserInfo user)
		{
			return this.Client.Put<SecurityUserInfo, SecurityUserInfo>(string.Format("user/{0}", id), this.Client.Accept, user);
		}

        /// <summary>
        /// Gets a list of two-factor mechanisms
        /// </summary>
        public AmiCollection<TfaMechanismInfo> GetTwoFactorMechanisms()
        {
            return this.Client.Get<AmiCollection<TfaMechanismInfo>>("tfa", null);
        }

        /// <summary>
        /// Create security password reset request
        /// </summary>
        public void SendTfaSecret(TfaRequestInfo resetInfo)
        {
            this.Client.Post<TfaRequestInfo, Object>("tfa", this.Client.Accept, resetInfo);
        }
	}
}