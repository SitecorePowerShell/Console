using System;
using System.Data;
using System.Management.Automation;
using System.Security.Principal;
using System.Web;
using System.Web.Security;
using Sitecore;
using Sitecore.Security.Accounts;
using Sitecore.SecurityModel.License;

namespace Cognifide.PowerShell.Commandlets.Security.Session
{
    [Cmdlet("Login", "User", SupportsShouldProcess = true)]
    public class LoginUserCommand : BaseCommand
    {
        [Parameter(Position = 0, Mandatory = true)]
        [Alias("UserName")]
        [ValidateNotNullOrEmpty]
        public GenericIdentity Identity { get; set; }

        [Parameter(Position = 1, Mandatory = true)]
        public string Password { get; set; }

        protected override void ProcessRecord()
        {
            RecoverHttpContext();

            var username = Identity.Name;

            if (!username.Contains(@"\") && !String.IsNullOrEmpty(username))
            {
                username = @"sitecore\" + username;
            }

            if (!User.Exists(username))
            {
                WriteError(new ErrorRecord(
                    new ObjectNotFoundException("User '" + username + "' could not be found"),
                    "user not found", ErrorCategory.ObjectNotFound, null));
            }

            if (ShouldProcess(username, "Login as user"))
            {
                if (Context.IsLoggedIn)
                {
                    if (Context.User.Name.Equals(username, StringComparison.OrdinalIgnoreCase)) return;
                    Context.Logout();
                }
                if (!LicenseManager.HasContentManager && !LicenseManager.HasExpress)
                {
                    WriteError(new ErrorRecord(new LicenseException("A required license is missing"),
                        "sitecore_license_missing", ErrorCategory.ResourceUnavailable, null));
                }
                if (!Membership.ValidateUser(username, Password))
                {
                    WriteError(new ErrorRecord(new LicenseException("Unknown username or password."),
                        "sitecore_invalid_login_info", ErrorCategory.PermissionDenied, null));
                }
                var user = User.FromName(username, true);

                UserSwitcher.Enter(user);

                SessionState.PSVariable.Set("me", HttpContext.Current.User.Identity.Name);
            }
        }
    }
}