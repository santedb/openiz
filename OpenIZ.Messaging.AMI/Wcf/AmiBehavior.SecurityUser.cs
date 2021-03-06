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

using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services.Security;
using OpenIZ.Core.Model.AMI.Auth;
using OpenIZ.Core.Model.AMI.Security;
using OpenIZ.Core.Model.Constants;
using OpenIZ.Core.Model.Query;
using OpenIZ.Core.Model.Security;
using OpenIZ.Core.Security;
using OpenIZ.Core.Security.Attribute;
using OpenIZ.Core.Security.Claims;
using OpenIZ.Core.Services;
using SwaggerWcf.Attributes;
using System;
using System.Data;
using System.Linq;
using System.Security;
using System.ServiceModel.Web;

namespace OpenIZ.Messaging.AMI.Wcf
{
	/// <summary>
	/// Represents the administrative contract interface.
	/// </summary>
	public partial class AmiBehavior
	{
		/// <summary>
		/// Changes the password of a user.
		/// </summary>
		/// <param name="id">The id of the user whose password is to be changed.</param>
		/// <param name="password">The new password of the user.</param>
		/// <returns>Returns the updated user.</returns>
		[SwaggerWcfTag("Administrative Management Interface (AMI)")]
		[SwaggerWcfSecurity("OAUTH2")]
		[SwaggerWcfResponse(503, "The AMI service is unavailable (for example: Server is still starting up, or didn't start up correctly)")]
		[SwaggerWcfResponse(403, "User attempted to perform an operation but they are unauthorized to do so")]
		[SwaggerWcfResponse(401, "Operation requires authentication")]
		[SwaggerWcfResponse(429, "The server throttling has been exceeded")]
		[SwaggerWcfResponse(400, "The client has made a request that this server cannot fulfill")]
		[SwaggerWcfResponse(405, "You are not allowed to perform this operation on this resource")]
		[SwaggerWcfResponse(501, "The method / operation you are calling is not implemented")]
		[SwaggerWcfResponse(404, "The provided resource could not be found")]
		[SwaggerWcfResponse(422, "The operation resulted in one or more business rules being violated")]
		[SwaggerWcfResponse(200, "The operation was successful, and the most recent version of the resource is in the response")]
		public SecurityUser ChangePassword(string id, string password)
		{
			Guid userKey = Guid.Empty;

			if (!Guid.TryParse(id, out userKey))
			{
				throw new ArgumentException($"{nameof(id)} must be a valid GUID");
			}

			var securityRepository = ApplicationContext.Current.GetService<ISecurityRepositoryService>();

			if (securityRepository == null)
			{
				throw new InvalidOperationException($"{nameof(ISecurityRepositoryService)} not found");
			}

			return securityRepository.ChangePassword(userKey, password);
		}

		/// <summary>
		/// Creates a security user.
		/// </summary>
		/// <param name="user">The security user to be created.</param>
		/// <returns>Returns the newly created security user.</returns>
		[SwaggerWcfTag("Administrative Management Interface (AMI)")]
		[SwaggerWcfSecurity("OAUTH2")]
		[SwaggerWcfResponse(503, "The AMI service is unavailable (for example: Server is still starting up, or didn't start up correctly)")]
		[SwaggerWcfResponse(400, "The client has made a request that this server cannot fulfill")]
		[SwaggerWcfResponse(401, "Operation requires authentication")]
		[SwaggerWcfResponse(403, "User attempted to perform an operation but they are unauthorized to do so")]
		[SwaggerWcfResponse(404, "The provided resource could not be found")]
		[SwaggerWcfResponse(405, "You are not allowed to perform this operation on this resource")]
		[SwaggerWcfResponse(409, "You are attempting to create a resource that already exists")]
		[SwaggerWcfResponse(422, "The operation resulted in one or more business rules being violated")]
		[SwaggerWcfResponse(429, "The server throttling has been exceeded")]
		[SwaggerWcfResponse(501, "The method / operation you are calling is not implemented")]
		[SwaggerWcfResponse(201, "The object was created successfully")]
		public SecurityUserInfo CreateUser(SecurityUserInfo user)
		{
			var userRepository = ApplicationContext.Current.GetService<ISecurityRepositoryService>();
			var roleProviderService = ApplicationContext.Current.GetService<IRoleProviderService>();

			var userToCreate = new SecurityUser
			{
				Email = user.Email,
				UserName = user.UserName,
			};

			if (user.User == null)
			{
				userToCreate.UserClass = UserClassKeys.HumanUser;
			}
			else
			{
				userToCreate.InvalidLoginAttempts = user.User.InvalidLoginAttempts;
				userToCreate.PhoneNumber = user.User.PhoneNumber;
				userToCreate.UserClass = user.User.UserClass == Guid.Empty ? UserClassKeys.HumanUser : user.User.UserClass;
			}

			if (user.Lockout.HasValue && user.Lockout.Value)
			{
				// to not overflow the date time value into 5 digit years
				userToCreate.Lockout = DateTime.MaxValue.AddYears(-100);
			}

			var securityUser = userRepository.CreateUser(userToCreate, user.Password);

			if (user.Roles?.Any() == true)
			{
				roleProviderService.AddUsersToRoles(new String[] { user.UserName }, user.Roles.Select(o => o.Name).ToArray(), AuthenticationContext.Current.Principal);
			}

			return new SecurityUserInfo(securityUser);
		}

		/// <summary>
		/// Deletes a security user.
		/// </summary>
		/// <param name="rawUserId">The id of the user to be deleted.</param>
		/// <returns>Returns the deleted user.</returns>
		[SwaggerWcfTag("Administrative Management Interface (AMI)")]
		[SwaggerWcfSecurity("OAUTH2")]
		[SwaggerWcfResponse(503, "The AMI service is unavailable (for example: Server is still starting up, or didn't start up correctly)")]
		[SwaggerWcfResponse(403, "User attempted to perform an operation but they are unauthorized to do so")]
		[SwaggerWcfResponse(401, "Operation requires authentication")]
		[SwaggerWcfResponse(429, "The server throttling has been exceeded")]
		[SwaggerWcfResponse(400, "The client has made a request that this server cannot fulfill")]
		[SwaggerWcfResponse(405, "You are not allowed to perform this operation on this resource")]
		[SwaggerWcfResponse(501, "The method / operation you are calling is not implemented")]
		[SwaggerWcfResponse(409, "You are attempting to perform an obsolete on an old version of the resource, or the conditional HTTP headers don't match the current version of the resource")]
		[SwaggerWcfResponse(404, "The provided resource could not be found")]
		[SwaggerWcfResponse(422, "The operation resulted in one or more business rules being violated")]
		[SwaggerWcfResponse(200, "The object was obsoleted successfully")]
		public SecurityUserInfo DeleteUser(string rawUserId)
		{
			var key = Guid.Empty;

			if (!Guid.TryParse(rawUserId, out key))
			{
				throw new ArgumentException($"{nameof(rawUserId)} must be a valid GUID");
			}

			var securityRepositoryService = ApplicationContext.Current.GetService<ISecurityRepositoryService>();

			if (securityRepositoryService == null)
			{
				throw new InvalidOperationException($"{nameof(ISecurityRepositoryService)} not found");
			}

			return new SecurityUserInfo(securityRepositoryService.ObsoleteUser(key));
		}

		/// <summary>
		/// Gets a specific security user.
		/// </summary>
		/// <param name="rawUserId">The raw user identifier.</param>
		/// <returns>Returns the security user.</returns>
		/// <exception cref="System.ArgumentException">rawUserId</exception>
		[SwaggerWcfTag("Administrative Management Interface (AMI)")]
		[SwaggerWcfSecurity("OAUTH2")]
		[SwaggerWcfResponse(503, "The AMI service is unavailable (for example: Server is still starting up, or didn't start up correctly)")]
		[SwaggerWcfResponse(403, "User attempted to perform an operation but they are unauthorized to do so")]
		[SwaggerWcfResponse(401, "Operation requires authentication")]
		[SwaggerWcfResponse(429, "The server throttling has been exceeded")]
		[SwaggerWcfResponse(400, "The client has made a request that this server cannot fulfill")]
		[SwaggerWcfResponse(405, "You are not allowed to perform this operation on this resource")]
		[SwaggerWcfResponse(501, "The method / operation you are calling is not implemented")]
		[SwaggerWcfResponse(404, "The provided resource could not be found")]
		[SwaggerWcfResponse(422, "The operation resulted in one or more business rules being violated")]
		[SwaggerWcfResponse(200, "The operation was successful, and the most recent version of the resource is in the response")]
		public SecurityUserInfo GetUser(string rawUserId)
		{
			Guid userId = Guid.Empty;
			if (!Guid.TryParse(rawUserId, out userId))
				throw new ArgumentException(nameof(rawUserId));
			var userRepository = ApplicationContext.Current.GetService<ISecurityRepositoryService>();
            var user = userRepository.GetUser(userId);
            if(WebOperationContext.Current.IncomingRequest.Headers["If-Match"] != null)
            {
                var ifMatch = WebOperationContext.Current.IncomingRequest.Headers["If-Match"];
                var hash = ApplicationContext.Current.GetService<IPasswordHashingService>().EncodePassword(ifMatch);
                if(user.SecurityHash != hash)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NotModified;
                    return null;
                }

                var expiration = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(DateTime.MaxValue.Subtract(user.Lockout.Value.ToUniversalTime()).TotalSeconds).ToLocalTime();
                if (DateTime.Now > expiration)
                    throw new SecurityException("Period for token has expired");


            }
            return new SecurityUserInfo(user);
		}

		/// <summary>
		/// Gets a list of security users.
		/// </summary>
		/// <returns>Returns a list of security users.</returns>
		[SwaggerWcfTag("Administrative Management Interface (AMI)")]
		[SwaggerWcfSecurity("OAUTH2")]
		[SwaggerWcfResponse(503, "The AMI service is unavailable (for example: Server is still starting up, or didn't start up correctly)")]
		[SwaggerWcfResponse(403, "User attempted to perform an operation but they are unauthorized to do so")]
		[SwaggerWcfResponse(401, "Operation requires authentication")]
		[SwaggerWcfResponse(429, "The server throttling has been exceeded")]
		[SwaggerWcfResponse(400, "The client has made a request that this server cannot fulfill")]
		[SwaggerWcfResponse(405, "You are not allowed to perform this operation on this resource")]
		[SwaggerWcfResponse(501, "The method / operation you are calling is not implemented")]
		[SwaggerWcfResponse(404, "The provided resource could not be found")]
		[SwaggerWcfResponse(422, "The operation resulted in one or more business rules being violated")]
		[SwaggerWcfResponse(200, "The operation was successful, and the most recent version of the resource is in the response")]
		public AmiCollection<SecurityUserInfo> GetUsers()
		{
			var expression = QueryExpressionParser.BuildLinqExpression<SecurityUser>(this.CreateQuery(WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters));
			var userRepository = ApplicationContext.Current.GetService<ISecurityRepositoryService>();
			return new AmiCollection<SecurityUserInfo>() { CollectionItem = userRepository.FindUsers(expression).Select(o => new SecurityUserInfo(o)).ToList() };
		}

		/// <summary>
		/// Updates a security user.
		/// </summary>
		/// <param name="rawUserId">The raw user identifier.</param>
		/// <param name="info">The security user containing the updated information.</param>
		/// <returns>Returns the updated security user.</returns>
		[SwaggerWcfTag("Administrative Management Interface (AMI)")]
		[SwaggerWcfSecurity("OAUTH2")]
		[SwaggerWcfResponse(503, "The AMI service is unavailable (for example: Server is still starting up, or didn't start up correctly)")]
		[SwaggerWcfResponse(403, "User attempted to perform an operation but they are unauthorized to do so")]
		[SwaggerWcfResponse(401, "Operation requires authentication")]
		[SwaggerWcfResponse(429, "The server throttling has been exceeded")]
		[SwaggerWcfResponse(400, "The client has made a request that this server cannot fulfill")]
		[SwaggerWcfResponse(405, "You are not allowed to perform this operation on this resource")]
		[SwaggerWcfResponse(501, "The method / operation you are calling is not implemented")]
		[SwaggerWcfResponse(404, "The provided resource could not be found")]
		[SwaggerWcfResponse(422, "The operation resulted in one or more business rules being violated")]
		[SwaggerWcfResponse(200, "The operation was successful, and the most recent version of the resource is in the response")]
		public SecurityUserInfo UpdateUser(string rawUserId, SecurityUserInfo info)
		{
			Guid userId = Guid.Parse(rawUserId);
			// First change password if needed
			var userRepository = ApplicationContext.Current.GetService<ISecurityRepositoryService>();
			var idpService = ApplicationContext.Current.GetService<IIdentityProviderService>();
			if (!String.IsNullOrEmpty(info.Password))
			{
				var user = userRepository.ChangePassword(userId, info.Password);
				idpService.RemoveClaim(user.UserName, OpenIzClaimTypes.OpenIZPasswordlessAuth);
			}

			if (info.UserName == null)
			{
				info.UserName = userRepository.GetUser(userId)?.UserName;
			}

			if (info.User != null)
				userRepository.SaveUser(info.User);

			if (info.Lockout.HasValue)
			{
				// Alter identity DEMAND
				new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.AlterIdentity).Demand();

				if (info.Lockout.Value)
					userRepository.LockUser(userId);
				else
					userRepository.UnlockUser(userId);
			}

			// First, we remove the roles
			if (info.Roles != null && info.Roles.Any())
			{
				// Alter identity DEMAND
				new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.AlterIdentity).Demand();


                // Roles? We want to update
                if (info.Roles != null && info.Roles.Count > 0)
                {
                    var irps = ApplicationContext.Current.GetService<IRoleProviderService>();
                    var exsitingRoles = irps.GetAllRoles(info.UserName);
                    irps.RemoveUsersFromRoles(new String[] { info.UserName }, exsitingRoles.Where(o => !info.Roles.Any(r=>r.Name.Equals(o, StringComparison.OrdinalIgnoreCase))).ToArray(), AuthenticationContext.Current.Principal);
                    irps.AddUsersToRoles(new string[] { info.UserName }, info.Roles.Where(o => !exsitingRoles.Contains(o.Name)).Select(o=>o.Name).ToArray(), AuthenticationContext.Current.Principal);
                }

			}

			return info;
		}
	}
}