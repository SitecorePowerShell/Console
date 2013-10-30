using System.Management.Automation;
using System.Web;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets
{
    [Cmdlet("Get", "UserAgent")]
    [OutputType(new[] {typeof (string)})]
    public class GetUserAgentCommand : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            if (HttpContext.Current != null)
            {
                WriteObject(HttpContext.Current.Request.UserAgent, false);
            }
        }
    }
}