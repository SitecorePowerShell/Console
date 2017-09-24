using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Web;
using System.Web.Caching;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Commandlets.Interactive
{
    [Cmdlet(VerbsData.Update, "ListView")]
    [OutputType(typeof(string))]
    public class UpdateListViewCommand : BaseListViewCommand
    {
        public class UpdateListViewData
        {
            public List<DataObject> CumulativeData { get; set; } = new List<DataObject>();
            public bool InfoTitleChange { get; set; }
            public string InfoTitle { get; set; }
            public bool InfoDescriptionChange { get; set; }
            public string InfoDescription { get; set; }
            public bool MissingDataMessageChange { get; set; }
            public string MissingDataMessage { get; set; }
            public bool MissingDataIconChange { get; set; }
            public string MissingDataIcon { get; set; }
            public bool IconChange { get; set; }
            public string Icon { get; set; }
        }

        public override string Title { get; set; }
        public override int Width { get; set; }
        public override int Height { get; set; }

        protected override void EndProcessing()
        {
            if (CheckSessionCanDoInteractiveAction())
            {
                var updateData = new UpdateListViewData
                {
                    CumulativeData = CumulativeData,
                    InfoDescriptionChange = IsParameterSpecified(nameof(InfoDescription)),
                    InfoDescription = InfoDescription,
                    InfoTitleChange = IsParameterSpecified(nameof(InfoTitle)),
                    InfoTitle = InfoTitle,
                    MissingDataMessageChange = IsParameterSpecified(nameof(MissingDataMessage)),
                    MissingDataMessage = MissingDataMessage,
                    MissingDataIconChange = IsParameterSpecified(nameof(MissingDataIcon)),
                    MissingDataIcon = MissingDataIcon,
                    IconChange = IsParameterSpecified(nameof(Icon)),
                    Icon = Icon
                };
                HttpRuntime.Cache.Add($"allDataInternal|{HostData.SessionId}", updateData, null,
                    Cache.NoAbsoluteExpiration, new TimeSpan(0, 1, 0), CacheItemPriority.Normal, null);
                var message = Message.Parse(null, "pslv:update");
                message.Arguments.Add("ScriptSession.Id", HostData.SessionId);
                PutMessage(new SendMessageMessage(message, false));
            }
            base.EndProcessing();
        }
    }
}