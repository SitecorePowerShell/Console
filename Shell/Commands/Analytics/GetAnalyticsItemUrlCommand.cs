using System.Data.Objects;
using System.Management.Automation;

namespace Cognifide.PowerShell.Shell.Commands.Analytics
{
    [Cmdlet("Get", "AnalyticsItemUrl")]
    public class GetAnalyticsItemUrlCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            ObjectQuery<ItemUrls> itemUrls = Context.ItemUrls;
            PipeQuery(itemUrls);
        }
    }
}