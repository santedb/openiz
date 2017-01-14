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
 * Date: 2016-6-14
 */
using System;

using OpenIZ.Core.Model.Security;
using PetaPoco;

namespace OpenIZ.Persistence.Data.PSQL.Data.Model.Security
{
	/// <summary>
	/// Represents a user for the purpose of authentication
	/// </summary>
	[TableName("sec_usr_tbl")]
	public class DbSecurityUser : DbNonVersionedBaseData
	{

		/// <summary>
		/// Gets or sets the email.
		/// </summary>
		/// <value>The email.</value>
		[Column("email")]
		public String Email {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the invalid login attempts.
		/// </summary>
		/// <value>The invalid login attempts.</value>
		[Column("fail_login")]
		public int InvalidLoginAttempts {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="OpenIZ.Mobile.Core.Data.Model.Security.DbSecurityUser"/> lockout enabled.
		/// </summary>
		/// <value><c>true</c> if lockout enabled; otherwise, <c>false</c>.</value>
		[Column("locked")]
		public DateTime? Lockout {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the password hash.
		/// </summary>
		/// <value>The password hash.</value>
		[Column("passwd")]
		public String PasswordHash {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the security stamp.
		/// </summary>
		/// <value>The security stamp.</value>
		[Column("sec_stmp")]
		public String SecurityHash {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is two factor enabled.
		/// </summary>
		/// <value><c>true</c> if this instance is two factor enabled; otherwise, <c>false</c>.</value>
		[Column("tfa_enabled")]
		public bool TwoFactorEnabled {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the name of the user.
		/// </summary>
		/// <value>The name of the user.</value>
		[Column("usr_name")]
		public String UserName {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the last login.
		/// </summary>
		/// <value>The last login.</value>
		[Column("last_login_utc")]
		public DateTime? LastLoginTime {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the phone number.
		/// </summary>
		/// <value>The phone number.</value>
		[Column("phn_num")]
		public String PhoneNumber {
			get;
			set;
		}

        /// <summary>
        /// User classification
        /// </summary>
        [Column("cls_id")]
        public Guid Userclass { get; set; }

        /// <summary>
        /// Email confirmed
        /// </summary>
        [Column("email_cnf")]
        public bool EmailConfirmed { get; set; }
        
        /// <summary>
        /// Phone confirmed
        /// </summary>
        [Column("phn_cnf")]
        public bool PhoneNumberConfirmed { get; set; }

        /// <summary>
        /// Gets or sets the user identifier
        /// </summary>
        [Column("usr_id")]
        public override Guid Key { get; set; }
    }

	/// <summary>
	/// Associative entity between security user and role
	/// </summary>
	[TableName("sec_usr_rol_assoc_tbl")]
	public class DbSecurityUserRole 
	{
        /// <summary>
        /// Gets or sets the user key
        /// </summary>
        [Column("usr_id")]
        public Guid UserKey { get; set; }

        /// <summary>
        /// Gets or sets the role key
        /// </summary>
        [Column("rol_id")]
        public Guid RoleKey { get; set; }

    }

    /// <summary>
    /// Represents an external login
    /// </summary>
    [TableName("sec_usr_extrn_lgn_tbl")]
    public class DbUserExternalLogin : IDbAssociation
    {
        /// <summary>
        /// Gets or sets the source key
        /// </summary>
        [Column("usr_id")]
        public Guid SourceKey { get; set; }

        /// <summary>
        /// The provider key for login
        /// </summary>
        [Column("pvd_key")]
        public String ProviderKey { get; set; }

        /// <summary>
        /// Provider name
        /// </summary>
        [Column("lgn_pvdr")]
        public String Provider { get; set; }
    }

    /// <summary>
    /// User claim
    /// </summary>
    public class DbUserClaim : DbAssociation
    {

        /// <summary>
        /// Gets or sets the primary key for the claim
        /// </summary>
        [Column("clm_id")]
        public override Guid Key { get; set; }

        /// <summary>
        /// Gets or sets the source key
        /// </summary>
        [Column("usr_id")]
        public override Guid SourceKey { get; set; }

        /// <summary>
        /// Claim type key
        /// </summary>
        [Column("clm_typ")]
        public String ClaimType { get; set; }

        /// <summary>
        /// Claim value 
        /// </summary>
        [Column("clm_val")]
        public String ClaimValue { get; set; }

    }
}

