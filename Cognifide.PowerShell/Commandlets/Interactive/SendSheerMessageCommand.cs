using System.Collections;
using System.Management.Automation;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Cognifide.PowerShell.Services;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Commandlets.Interactive
{
    [Cmdlet(VerbsCommunications.Send, "SheerMessage")]
    public class SendSheerMessageCommand : BaseShellCommand
    {
        [Parameter(ValueFromPipeline = true, Position = 0, Mandatory = true)]
        public string Name { get; set; }

        [Parameter]
        public SwitchParameter GetResult { get; set; }

        [Parameter]
        public Hashtable Parameters { get; set; }

        protected override void ProcessRecord()
        {
            if (!CheckSessionCanDoInteractiveAction()) return;

            var message = Message.Parse(null, Name);
            message.Arguments.Add("ScriptSession.Id", HostData.SessionId);
            if (Parameters != null)
            {
                foreach (var key in Parameters.Keys)
                {
                    message.Arguments.Add(key.ToString(), Parameters[key].ToString());
                }
            }
            var msgHandler = new SendMessageMessage(message, GetResult.IsPresent);
            LogErrors(() => PutMessage(msgHandler));
            var jobManager = TypeResolver.Resolve<IJobManager>();
            var job = jobManager.GetContextJob();
            if (job != null && GetResult.IsPresent)
            {
                WriteObject(job.MessageQueue.GetResult());
            }
        }
    }
}