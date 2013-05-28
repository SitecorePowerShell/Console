using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Analytics
{
    [Cmdlet("Get", "AnalyticsVisitorTag")]
    public class GetAnalyticsVisitorTagCommand : AnalyticsBaseCommand
    {
        #region Methods

        protected override void ProcessRecord()
        {
            ObjectQuery<VisitorTags> visitorTags = Context.VisitorTags;
            PipeQuery(visitorTags);
        }

        #endregion
    }
}