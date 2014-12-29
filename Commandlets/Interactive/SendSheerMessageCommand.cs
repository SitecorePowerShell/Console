using System.Management.Automation;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Commandlets.Interactive
{
    [Cmdlet(VerbsCommunications.Send, "SheerMessage")]
    public class SendSheerMessageCommand : BaseShellCommand
    {
        [Parameter(ValueFromPipeline = true, Position = 0, Mandatory = true)]
        public string Name { get; set; }

        public SwitchParameter GetResult { get; set; }

        protected override void ProcessRecord()
        {
            var message = Message.Parse(null, Name);
            message.Arguments.Add("ScriptSession.Id", HostData.SessionId);
            var msgHandler = new SendMessageMessage(message, GetResult.IsPresent);
            LogErrors(() => PutMessage(msgHandler));
            if (JobContext.IsJob && GetResult.IsPresent)
            {
                WriteObject(JobContext.MessageQueue.GetResult());
            }
            //return message.MessageQueue.GetResult();
        }
    }
}