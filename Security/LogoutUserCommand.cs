using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets;
using Sitecore.Security.Authentication;

namespace Cognifide.PowerShell.Security
{
    [Cmdlet("Logout", "User")]
    public class LogoutUserCommand : BaseCommand
    {
        protected override void ProcessRecord()
        {
            RecoverHttpContext();

            AuthenticationManager.Logout();

            SessionState.PSVariable.Set("me", string.Empty);
        }
    }
}