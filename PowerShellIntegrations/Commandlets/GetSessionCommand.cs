using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Web;
using Sitecore.Data.Indexing;
using Sitecore.Web.Authentication;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets
{
    [Cmdlet("Get", "Session")]
    [OutputType(new[] { typeof(Index) })]
    public class GetSessionCommand : BaseCommand
    {
        [Parameter]
        public string User { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            WildcardWrite(User, DomainAccessGuard.Sessions, s=> s.UserName);
        }
    }
}