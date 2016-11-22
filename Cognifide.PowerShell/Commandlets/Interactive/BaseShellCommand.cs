using Cognifide.PowerShell.Commandlets.Interactive.Messages;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Jobs.AsyncUI;

namespace Cognifide.PowerShell.Commandlets.Interactive
{
    public class BaseShellCommand : BaseCommand
    {
        protected override void BeginProcessing()
        {
            LogErrors(EnsureSiteContext);
        }

        public static void EnsureSiteContext()
        {
            if (JobContext.IsJob)
                Context.Site = Factory.GetSite(Context.Job.Options.SiteName);
        }

        public static void PutMessage(IMessage message)
        {
            if (JobContext.IsJob)
            {
                JobContext.MessageQueue.PutMessage(message);
            }
            else
            {
                message.Execute();
            }
        }
    }
}