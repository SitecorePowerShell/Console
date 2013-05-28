using System;
using System.Management.Automation;
using System.Web;
using System.Web.Security;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Security.Accounts;
using Sitecore.SecurityModel.License;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets
{
    [Cmdlet("Login", "User")]
    public class LoginUserCommand : BaseCommand
    {
        [Parameter(Position = 0, Mandatory = true)]
        public string UserName { get; set; }

        [Parameter(Position = 1, Mandatory = true)]
        public string Password { get; set; }

        [Parameter]
        public SwitchParameter Remember { get; set; }

        protected override void ProcessRecord()
        {
            RecoverHttpContext();

            if (!UserName.Contains("\\"))
            {
                UserName = "sitecore\\" + UserName;
            }
            if (Context.IsLoggedIn)
            {
                if (Context.User.Name.Equals(UserName, StringComparison.OrdinalIgnoreCase))
                    return;
                Context.Logout();
            }
            if (!LicenseManager.HasContentManager && !LicenseManager.HasExpress)
            {
                throw new LicenseException("A required license is missing");
            }
            Assert.IsTrue(Membership.ValidateUser(UserName, Password), "Unknown username or password.");
            User user = User.FromName(UserName, true);
/*
            if (!user.IsAdministrator && !user.IsInRole(Role.FromName("sitecore\\Sitecore Client Developing")))
                throw new Exception("User is not an Administrator or a member of the sitecore\\Sitecore Client Developing role");
*/
            UserSwitcher.Enter(user);

            SessionState.PSVariable.Set("me", HttpContext.Current.User.Identity.Name);

/*            bool loggedIn = Sitecore.Security.Authentication.AuthenticationManager.Login(
        Login1.UserName, Login1.Password);
            if (!loggedIn)
            {
                e.Authenticated = Sitecore.Security.Authentication.AuthenticationManager.Login(
                    "sitecore\\" + Login1.UserName, Login1.Password, Login1.RememberMeSet);
            }

            /*
            if (Sitecore.Security.Accounts.User.Exists(domainUser))
            {
                Sitecore.Security.Accounts.User user =
                  Sitecore.Security.Accounts.User.FromName(domainUser, false);
                Sitecore.Security.Accounts.User.Current
                using (new Sitecore.Security.Accounts.UserSwitcher(user))
                {
                    //TODO: code to invoke as user 
                }
            } 
             */
        }
    }
}