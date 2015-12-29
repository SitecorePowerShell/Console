using System.Management.Automation;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Commandlets.Interactive
{
    [Cmdlet(VerbsData.Update, "ListView")]
    [OutputType(typeof (string))]
    public class UpdateListViewCommand : BaseListViewCommand
    {
        public override string Title { get; set; }
        public override int Width { get; set; }
        public override int Height { get; set; }

        protected override void EndProcessing()
        {
            if (CheckSessionCanDoInteractiveAction())
            {
                LogErrors(() => SessionState.PSVariable.Set("allDataInternal", CumulativeData));
                var message = Message.Parse(null, "pslv:update");
                message.Arguments.Add("ScriptSession.Id", HostData.SessionId);
                PutMessage(new SendMessageMessage(message, false));
            }
            base.EndProcessing();
        }
    }
}