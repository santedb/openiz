using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Event;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.Services.Security;
using OpenIZ.Core.Configuration;
using OpenIZ.Core.Diagnostics;
using OpenIZ.Core.Model.Patch;
using OpenIZ.Core.Model.Security;
using OpenIZ.Core.Notifications;
using OpenIZ.Core.Security.Notification;
using OpenIZ.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenIZ.Core.Security.Notification
{
    /// <summary>
    /// Represents a user event notification service
    /// </summary>
    public class UserEventNotifier : IIdentityProviderService, IRoleProviderService
    {

        // Password list
        private string[] m_passwordList;

        // Symbols
        private readonly string[] m_symbols = { "@", "!", "_", "#", "%" };

        // Random seed
        private Random m_random = new Random();

        private Regex m_bindingRegex = new Regex("{{\\s?\\$([A-Za-z0-9_]*?)\\s?}}");

        private OpenIzConfiguration m_configuration = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection(OpenIzConstants.OpenIZConfigurationName) as OpenIzConfiguration;

        // Trace source
        private TraceSource m_traceSource = new TraceSource("OpenIZ.Core.Security");

        // Identity service
        private IIdentityProviderService m_identityService;

        // Role service
        private IRoleProviderService m_roleService;

        // Security service
        private ISecurityRepositoryService m_securityService;
        private IPasswordHashingService m_hashService;

        /// <summary>
        /// Authenticating
        /// </summary>
        public event EventHandler<AuthenticatingEventArgs> Authenticating;
        /// <summary>
        /// Authenticated
        /// </summary>
        public event EventHandler<AuthenticatedEventArgs> Authenticated;

        /// <summary>
        /// User event notification service
        /// </summary>
        public UserEventNotifier()
        {
            ApplicationContext.Current.Started += (o, e) =>
            {
                var identityService = ApplicationContext.Current.GetService<IIdentityProviderService>();
                // Replace the existing with myself
                this.m_identityService = identityService;
                this.m_identityService.Authenticated += M_identityService_Authenticated;
                this.m_identityService.Authenticating += M_identityService_Authenticating;
                var roleService = ApplicationContext.Current.GetService<IRoleProviderService>();
                this.m_roleService = roleService;
                this.m_securityService = ApplicationContext.Current.GetService<ISecurityRepositoryService>();
                this.m_hashService = ApplicationContext.Current.GetService<IPasswordHashingService>();

                // Repository service
                ApplicationContext.Current.GetService<IDataPersistenceService<SecurityUser>>().Updating += UserEventNotifier_Updating;
                ApplicationContext.Current.GetService<IDataPersistenceService<SecurityUser>>().Obsoleted += UserEventNotifier_Obsoleted;

                // Insert myself 
                var serviceInstances = ApplicationContext.Current.GetServices() as List<Object>;
                ApplicationContext.Current.RemoveServiceProvider(typeof(IIdentityProviderService));
                ApplicationContext.Current.RemoveServiceProvider(typeof(IRoleProviderService));
                serviceInstances.Remove(this);
                serviceInstances.Insert(0, this);

                // Re-add the replaced services, just in case they implement other interfaces
                serviceInstances.Add(identityService);
                serviceInstances.Add(roleService);
            };
        }

        /// <summary>
        /// Generates a random password
        /// </summary>
        /// <returns></returns>
        private String GenerateRandomPassword()
        {
            if (this.m_passwordList == null)
                this.m_passwordList = File.ReadAllLines(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "PasswordSeed.txt"));

            // Pick a random word
            var passwordText = this.m_passwordList[this.m_random.Next(0, this.m_passwordList.Length - 1)];
            // Generate a few numbers
            var numbers = new String(Enumerable.Range(0, 12 - passwordText.Length).Select(o => this.m_random.Next(0, 9).ToString()[0]).ToArray());
            // Generate a symbol
            var symbol = m_symbols[this.m_random.Next(0, this.m_symbols.Length)];
            return $"{symbol}{passwordText}{numbers}";
        }

        /// <summary>
        /// Obsoleting a user
        /// </summary>
        private void UserEventNotifier_Obsoleted(object sender, PostPersistenceEventArgs<SecurityUser> e)
        {
            var relay = NotificationRelayUtil.GetNotificationRelay($"mailto:");
            var fields = this.GetTemplateFields(e.Data);

            // Notify an administrator 
            var diff = new Patch()
            {
                AppliesTo = new PatchTarget(e.Data),
                CreatedBy = this.m_securityService.GetUser(e.Principal.Identity),
                CreationTime = DateTime.Now,
                Operation = new List<PatchOperation>()
                {
                    new PatchOperation(PatchOperationType.Replace, "ObsoletionTime", e.Data.ObsoletionTime),
                    new PatchOperation(PatchOperationType.Replace, "ObsoletedBy", e.Data.ObsoletedByKey)
                }
            };
            var adminMessage = this.FillTemplate("AdminUserObsoleteNotification", fields);
            foreach (var contact in this.m_configuration.Notification.AdminContacts)
                relay.Send($"mailto:{contact}", adminMessage.SubjectLine, adminMessage.BodyText, null, new Dictionary<String, String>() { { "changelog.txt", diff.ToString() } });
        }

        /// <summary>
        /// The user object is being updated
        /// </summary>
        private void UserEventNotifier_Updating(object sender, PrePersistenceEventArgs<SecurityUser> e)
        {
            var currentVersion = this.m_securityService.GetUser(e.Data.Key.Value);
            var relay = NotificationRelayUtil.GetNotificationRelay($"mailto:{currentVersion.Email}");
            var fields = this.GetTemplateFields(currentVersion);

            // Email has changed, notify the user on the old e-mail and new e-mail
            if (currentVersion.Email != e.Data.Email)
            {
                // Get the epoch value of expiration of this link
                var expiry = DateTime.Now.AddHours(2);
                var epoch = expiry.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
                // Set the lockout and email confirmation to false on new version
                e.Data.Lockout = DateTime.MaxValue.AddSeconds(-epoch.TotalSeconds);
                var token = BitConverter.ToString(currentVersion.Key.Value.ToByteArray()).Replace("-","") + BitConverter.ToString(Guid.NewGuid().ToByteArray()).Replace("-", "");
                e.Data.SecurityHash = this.m_hashService.EncodePassword(token);
                e.Data.EmailConfirmed = false;

                // Now send an email
                fields.Add("newEmail", e.Data.Email);
                fields.Add("authenticatedUser", e.Principal.Identity.Name);
                fields.Add("activateToken", token);
                fields.Add("expiry", expiry.ToString());
                // did the user change their own address?
                if (currentVersion.LastLoginTime.HasValue) // user has logged in and e-mail is changing 
                {
                    var message = this.FillTemplate("EmailChangedNotification", fields);
                    relay.Send($"mailto:{currentVersion.Email}", message.SubjectLine, message.BodyText);
                    message = this.FillTemplate("EmailChanged", fields);
                    relay.Send($"mailto:{e.Data.Email}", message.SubjectLine, message.BodyText);
                }
                else
                {
                    // Generates a random password and assigns to the user
                    var tPassword = this.GenerateRandomPassword();
                    this.m_identityService.ChangePassword(e.Data.UserName, tPassword, AuthenticationContext.SystemPrincipal);
                    fields.Add("password", tPassword);
                    e.Data.PasswordHash = null;
                    var message = this.FillTemplate("AccountActivation", fields);
                    relay.Send($"mailto:{e.Data.Email}", message.SubjectLine, message.BodyText);
                }

            }

            // Notify an administrator 
            // Was this a create operation?
            if (currentVersion.LastLoginTime.HasValue)
            {
                var diff = ApplicationContext.Current.GetService<IPatchService>().Diff(currentVersion, e.Data, "securityStamp", "passwordHash", "obsoletedBy", "id", "lastLoginTime", "updatedTime", "creationTime", "obsoletionTime", "createdBy", "updatedBy");
                if (diff.Operation.Any(o => o.OperationType != PatchOperationType.Test))
                {
                    var adminMessage = this.FillTemplate("AdminUserChangeNotification", fields);
                    foreach (var contact in this.m_configuration.Notification.AdminContacts)
                        relay.Send($"mailto:{contact}", adminMessage.SubjectLine, adminMessage.BodyText, null, new Dictionary<String, String>() { { "changelog.txt", diff.ToString() } });
                }
            }
        }


        /// <summary>
        /// Authenticating passthrough
        /// </summary>
        private void M_identityService_Authenticating(object sender, AuthenticatingEventArgs e) => this.Authenticating?.Invoke(this, e);

        /// <summary>
        /// Authenticated passthrough
        /// </summary>
        private void M_identityService_Authenticated(object sender, AuthenticatedEventArgs e) => this.Authenticated?.Invoke(this, e);

        /// <summary>
        /// Security user fields 
        /// </summary>
        private IDictionary<String, String> GetTemplateFields(SecurityUser e)
        {
            var retVal = new Dictionary<String, String>();
            foreach (var p in typeof(SecurityUser).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                retVal.Add(p.Name, p.GetValue(e, null)?.ToString());
            }
            return retVal;
        }

        /// <summary>
        /// Fill the specified template
        /// </summary>
        private NotificationTemplate FillTemplate(String templateName, IDictionary<String, String> templateFields)
        {
            NotificationTemplate retVal = null;
            using (var fs = File.OpenRead(Path.ChangeExtension(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "templates", templateName), "xml")))
                retVal = NotificationTemplate.Load(fs);

            // Process headers
            retVal.SubjectLine = this.ReplaceTemplate(retVal.SubjectLine, templateFields);
            retVal.BodyText = this.ReplaceTemplate(retVal.BodyText, templateFields);

            return retVal;
        }

        /// <summary>
        /// Replace template
        /// </summary>
        private string ReplaceTemplate(string source, IDictionary<string, string> templateFields)
        {
            return this.m_bindingRegex.Replace(source, (m) => templateFields.TryGetValue(m.Groups[1].Value, out string v) ? v : m.ToString());
        }

        /// <summary>
        /// Get the specified identity
        /// </summary>
        public IIdentity GetIdentity(string userName) => this.m_identityService.GetIdentity(userName);

        /// <summary>
        /// Create identity
        /// </summary>
        public IIdentity CreateIdentity(string userName, string password, IPrincipal authContext) => this.m_identityService.CreateIdentity(userName, password, authContext);
    
        /// <summary>
        /// Authenticate the user
        /// </summary>
        public IPrincipal Authenticate(string userName, string password) => this.m_identityService.Authenticate(userName, password);

        /// <summary>
        /// Authenticate
        /// </summary>
        public IPrincipal Authenticate(string userName, string password, string tfaSecret)
        {
            // TODO: Validate the TFA Secret equals the users's security hash , if not proceed as normal
            if (AuthenticationContext.Current.Principal == AuthenticationContext.AnonymousPrincipal)
                AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);
            var user = this.m_securityService.GetUser(userName);
            if (user.SecurityHash == this.m_hashService.EncodePassword(tfaSecret))
            {
                try
                {
                    // Unlock the user
                    this.m_identityService.SetLockout(userName, false, AuthenticationContext.Current.Principal);
                    user = this.m_securityService.GetUser(userName);
                    user.EmailConfirmed = true;
                    this.m_securityService.SaveUser(user);
                    return this.m_identityService.Authenticate(userName, password);
                    // Login was successful
                }
                catch (Exception e)
                {
                    this.m_traceSource.TraceError("Could not activate user account - {0}", e);
                    user.Lockout = DateTime.MaxValue;
                    this.m_securityService.SaveUser(user);
                    throw;
                }
            }
            else
                return this.m_identityService.Authenticate(userName, password, tfaSecret);
        }

        /// <summary>
        /// Change password
        /// </summary>
        public void ChangePassword(string userName, string newPassword, IPrincipal authContext)
        {
            this.m_identityService.ChangePassword(userName, newPassword, authContext);

            // TODO: Send e-mail for password change notification
            var identity = this.m_identityService.GetIdentity(userName);
            var user = this.m_securityService.GetUser(identity);
            if (!String.IsNullOrEmpty(user.Email) && authContext != AuthenticationContext.SystemPrincipal) // Notify the user that their password was changed
            {
                // Now send an email
                var templateFields = this.GetTemplateFields(user);
                templateFields.Add("authenticatedUser", authContext.Identity.Name);
                var message = this.FillTemplate("AccountPasswordChanged", templateFields);
                var relay = NotificationRelayUtil.GetNotificationRelay($"mailto:{user.Email}");
                relay.Send($"mailto:{user.Email}", message.SubjectLine, message.BodyText);
            }
        }


        /// <summary>
        /// Generate a TFA secret
        /// </summary>
        public string GenerateTfaSecret(string userName) => this.m_identityService.GenerateTfaSecret(userName);

        /// <summary>
        /// Delete identity
        /// </summary>
        public void DeleteIdentity(string userName, IPrincipal authContext)
        {
            this.m_identityService.DeleteIdentity(userName, authContext);

            // TODO: Send e-mail for deleted entity notification
        }

        /// <summary>
        /// Set lockout for the user
        /// </summary>
        public void SetLockout(string userName, bool lockout, IPrincipal authContext)
        {
            var user = this.m_securityService.GetUser(userName);
            this.m_identityService.SetLockout(userName, lockout, authContext);

            if (lockout && !user.Lockout.HasValue) // notify admin contacts of lockout condition
            {
                user = this.m_securityService.GetUser(userName);
                var templateFields = this.GetTemplateFields(user);
                templateFields.Add("authenticatedUser", authContext.Identity.Name);
                var message = this.FillTemplate("AccountLocked", templateFields);
                var relay = NotificationRelayUtil.GetNotificationRelay($"mailto:{user.Email}");
                relay.Send($"mailto:{user.Email}", message.SubjectLine, message.BodyText);
            }
        }

        /// <summary>
        /// Add a claim to the user
        /// </summary>
        public void AddClaim(string userName, Claim claim) => this.m_identityService.AddClaim(userName, claim);

        /// <summary>
        /// Remove a user claim
        /// </summary>
        public void RemoveClaim(string userName, string claimType) => this.m_identityService.RemoveClaim(userName, claimType);

        /// <summary>
        /// Create a role
        /// </summary>
        public void CreateRole(string roleName, IPrincipal authPrincipal)
        {
            this.m_roleService.CreateRole(roleName, authPrincipal);
        }

        /// <summary>
        /// Add users to roles
        /// </summary>
        public void AddUsersToRoles(string[] users, string[] roles, IPrincipal authPrincipal)
        {
            this.m_roleService.AddUsersToRoles(users, roles, authPrincipal);

            if (roles.Any(r => "Administrators".Equals(r, StringComparison.OrdinalIgnoreCase)))
            {
                var templateFields = new Dictionary<String, String>();
                templateFields.Add("users", String.Join("</li><li>", users));
                templateFields.Add("authenticatedUser", authPrincipal.Identity.Name);
                var message = this.FillTemplate("AdminAdministratorAddedNotification", templateFields);
                var relay = NotificationRelayUtil.GetNotificationRelay($"mailto:");
                foreach (var contact in this.m_configuration.Notification.AdminContacts)
                    relay.Send($"mailto:{contact}", message.SubjectLine, message.BodyText);
            }
        }

        /// <summary>
        /// Remove users from roles
        /// </summary>
        public void RemoveUsersFromRoles(string[] users, string[] roles, IPrincipal authPrincipal)
        {
            this.m_roleService.RemoveUsersFromRoles(users, roles, authPrincipal);

            if (roles.Any(r => "Administrators".Equals(r, StringComparison.OrdinalIgnoreCase)))
            {
                var templateFields = new Dictionary<String, String>();
                templateFields.Add("users", String.Join("</li><li> ", users));
                templateFields.Add("authenticatedUser", authPrincipal.Identity.Name);
                var message = this.FillTemplate("AdminAdministratorRemovedNotification", templateFields);
                var relay = NotificationRelayUtil.GetNotificationRelay($"mailto:");
                foreach (var contact in this.m_configuration.Notification.AdminContacts)
                    relay.Send($"mailto:{contact}", message.SubjectLine, message.BodyText);
            }
        }

        /// <summary>
        /// Find all users in role
        /// </summary>
        public string[] FindUsersInRole(string role) => this.m_roleService.FindUsersInRole(role);

        /// <summary>
        /// Get all roles
        /// </summary>
        public string[] GetAllRoles() => this.m_roleService.GetAllRoles();

        /// <summary>
        /// Get all roles for user
        /// </summary>
        public string[] GetAllRoles(string userName) => this.m_roleService.GetAllRoles(userName);

        /// <summary>
        /// Is user in role
        /// </summary>
        public bool IsUserInRole(string userName, string roleName) => this.m_roleService.IsUserInRole(userName, roleName);

        /// <summary>
        /// Is user in role
        /// </summary>
        public bool IsUserInRole(IPrincipal principal, string roleName) => this.m_roleService.IsUserInRole(principal, roleName);
    }
}
