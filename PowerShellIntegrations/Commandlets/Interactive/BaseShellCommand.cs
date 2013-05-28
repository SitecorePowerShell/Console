using Sitecore;
using Sitecore.Configuration;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    public class BaseShellCommand : BaseCommand
    {
        protected override void BeginProcessing()
        {
            LogErrors(() =>
                {
                    Context.Site = Factory.GetSite(Context.Job.Options.SiteName);
                });
        }
    }
}