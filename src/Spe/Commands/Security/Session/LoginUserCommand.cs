using System;
using System.Data;
using System.Management.Automation;
using System.Security.Principal;
using System.Web;
using Sitecore;
using Sitecore.Security.Accounts;
using Sitecore.SecurityModel.License;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Core.VersionDecoupling;

namespace Spe.Commands.Security.Session
{
    [Cmdlet("Login", "User", SupportsShouldProcess = true)]
    [Obsolete("This Cmdlet is deprecated and defunct. It will be removed in the subsequent version of SPE.")]
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
            // this functionality is deprecated and will be removed in the subsequent version of SPE.
            // it was broken since 2015: https://github.com/SitecorePowerShell/Console/issues/557
            // If you need this functionality use AuthenticationManager directly
        }
    }
}