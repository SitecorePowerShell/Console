using System;
using System.Management.Automation;
using Sitecore.Security.Authentication;

namespace Spe.Commands.Security.Session
{
    [Cmdlet("Logout", "User", SupportsShouldProcess = true)]
    [Obsolete("This Cmdlet is deprecated and defunct. It will be removed in the subsequent version of SPE.")]
    public class LogoutUserCommand : BaseCommand
    {
        protected override void ProcessRecord()
        {
            // this functionality is deprecated and will be removed in the subsequent version of SPE.
            // it was broken since 2015: https://github.com/SitecorePowerShell/Console/issues/557
            // If you need this functionality AuthenticationManager directly
        }
    }
}