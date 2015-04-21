using System.Management.Automation;
using Sitecore.Security.Authentication;

namespace Cognifide.PowerShell.Commandlets.Security.Session
{
    [Cmdlet("Logout", "User", SupportsShouldProcess = true)]
    public class LogoutUserCommand : BaseCommand
    {
        protected override void ProcessRecord()
        {
            RecoverHttpContext();

            if (ShouldProcess(SessionState.PSVariable.Get("me").Value.ToString(), "Logout user"))
            {
                AuthenticationManager.Logout();

                SessionState.PSVariable.Set("me", string.Empty);
            }
        }
    }
}