using System.Management.Automation;
using System.Web;

namespace Spe.Commands.Session
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