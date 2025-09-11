using System.Collections;
using System.Management.Automation;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Web.UI.Sheer;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Core.Extensions;
using Spe.Core.VersionDecoupling;

namespace Spe.Commands.Interactive
{
    [Cmdlet(VerbsCommunications.Send, "SheerMessage")]
    public class SendSheerMessageCommand : BaseShellCommand
    {
        [Parameter(ValueFromPipeline = true, Position = 0, Mandatory = true)]
        public string Name { get; set; }

        [Parameter(ParameterSetName = "InRunner")]
        public SwitchParameter GetResult { get; set; }

        [Parameter]
        public Hashtable Parameters { get; set; }

        [Parameter(ParameterSetName = "OnScriptEnd")]
        public SwitchParameter OnScriptEnd { get; set; }
        
        protected override void ProcessRecord()
        {
            if (!OnScriptEnd && !CheckSessionCanDoInteractiveAction()) return;

            var message = Message.Parse(null, Name);
            message.Arguments.Add("ScriptSession.Id", HostData.SessionId);
            if (Parameters != null)
            {
                foreach (var key in Parameters.Keys)
                {
                    message.Arguments.Add(key.ToString(), Parameters[key]?.ToString());
                }
            }
            var msgHandler = new SendMessageMessage(message, GetResult.IsPresent);
            if (OnScriptEnd)
            {
                HostData.DeferredMessages.Add($"message:{message.Serialize()}");
            }
            else
            {
                LogErrors(() => PutMessage(msgHandler));
                var jobManager = TypeResolver.ResolveFromCache<IJobManager>();
                var job = jobManager.GetContextJob();
                if (job != null && GetResult.IsPresent)
                {
                    WriteObject(job.MessageQueue.GetResult());
                }
            }
        }
    }
}