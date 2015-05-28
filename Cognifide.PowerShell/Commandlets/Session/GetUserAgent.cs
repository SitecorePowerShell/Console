using System.Management.Automation;
using System.Web;

namespace Cognifide.PowerShell.Commandlets.Session
{
    [Cmdlet("Get", "UserAgent")]
    [OutputType(typeof (string))]
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