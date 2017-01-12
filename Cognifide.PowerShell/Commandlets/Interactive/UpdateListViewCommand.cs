using System;
using System.Management.Automation;
using System.Web;
using System.Web.Caching;
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
                HttpRuntime.Cache.Add($"allDataInternal|{HostData.SessionId}", CumulativeData, null, Cache.NoAbsoluteExpiration,
                    new TimeSpan(0, 1, 0), CacheItemPriority.Normal, null);
                var message = Message.Parse(null, "pslv:update");
                message.Arguments.Add("ScriptSession.Id", HostData.SessionId);
                PutMessage(new SendMessageMessage(message, false));
            }
            base.EndProcessing();
        }
    }
}