using System.Management.Automation;
using Sitecore.Security.Authentication;

namespace Cognifide.PowerShell.Commandlets.Security
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