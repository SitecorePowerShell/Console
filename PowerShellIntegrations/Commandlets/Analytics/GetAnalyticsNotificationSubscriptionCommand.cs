using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Analytics
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