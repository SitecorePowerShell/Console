using Sitecore;
using Sitecore.Configuration;
using Sitecore.Jobs.AsyncUI;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    public class BaseShellCommand : BaseCommand
    {
        protected override void BeginProcessing()
        {
            LogErrors(() =>
                {
                    if (JobContext.IsJob)
                        Context.Site = Factory.GetSite(Context.Job.Options.SiteName);
                });
        }
        
        public void PutMessage(IMessage message)
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

        protected void FlushMessages()
        {
            if (JobContext.IsJob)
            {
                JobContext.Flush();
            }
        }

    }
}