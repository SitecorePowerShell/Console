using System;
using System.Management.Automation;
using System.Web;
using System.Web.Security;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Security.Accounts;
using Sitecore.SecurityModel.License;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Session
{
    [Cmdlet("Login", "User")]
    public class LoginUserCommand : BaseCommand
    {
        [Parameter(Position = 0, Mandatory = true)]
        [Alias("UserName")]
        public string Identity { get; set; }

        [Parameter(Position = 1, Mandatory = true)]
        public string Password { get; set; }

        [Parameter]
        public SwitchParameter Remember { get; set; }

        protected override void ProcessRecord()
        {
            RecoverHttpContext();

            if (!Identity.Contains("\\"))
            {
                Identity = "sitecore\\" + Identity;
            }
            if (Context.IsLoggedIn)
            {
                if (Context.User.Name.Equals(Identity, StringComparison.OrdinalIgnoreCase))
                    return;
                Context.Logout();
            }
            if (!LicenseManager.HasContentManager && !LicenseManager.HasExpress)
            {
                WriteError(new ErrorRecord(new LicenseException("A required license is missing"),
                    "sitecore_license_missing", ErrorCategory.ResourceUnavailable, null));
            }
            if (!Membership.ValidateUser(Identity, Password))
            {
                WriteError(new ErrorRecord(new LicenseException("Unknown username or password."),
                    "sitecore_invalid_login_info", ErrorCategory.PermissionDenied, null));
            }
            User user = User.FromName(Identity, true);
            /*
            if (!user.IsAdministrator && !user.IsInRole(Role.FromName("sitecore\\Sitecore Client Developing")))
                WriteError(new ErrorRecord(new LicenseException("User is not an Administrator or a member of the sitecore\\Sitecore Client Developing role"),
                                "sitecore_invalid_login_info", ErrorCategory.PermissionDenied, null));
            else
            */
            UserSwitcher.Enter(user);

            SessionState.PSVariable.Set("me", HttpContext.Current.User.Identity.Name);
        }
    }
}