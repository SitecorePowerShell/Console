using System.Management.Automation;

namespace Cognifide.PowerShell.Shell.Commands.Analytics
{
    [Cmdlet("Get", "AnalyticsNotificationSubscription")]
    public class GetAnalyticsNotificationSubscriptionCommand : AnalyticsBaseCommand
    {
        protected override void ProcessRecord()
        {
            PipeQuery(Context.NotificationSubscriptions);
        }
    }
}