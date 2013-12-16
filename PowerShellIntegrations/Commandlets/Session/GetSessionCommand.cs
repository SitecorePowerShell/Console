using System.Management.Automation;
using Sitecore.Data.Indexing;
using Sitecore.Web.Authentication;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Session
{
    [Cmdlet("Get", "Session")]
    [OutputType(new[] {typeof (Index)})]
    public class GetSessionCommand : BaseCommand
    {
        [Parameter]
        public string User { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            WildcardWrite(User, DomainAccessGuard.Sessions, s => s.UserName);
        }
    }
}