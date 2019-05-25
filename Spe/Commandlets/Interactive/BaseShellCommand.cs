using System.Management.Automation;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Jobs.AsyncUI;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Core.Extensions;
using Spe.Core.VersionDecoupling;

namespace Spe.Commandlets.Interactive
{
    public class BaseShellCommand : BaseCommand
    {
        protected override void BeginProcessing()
        {
            LogErrors(EnsureSiteContext);
        }

        public static void EnsureSiteContext()
        {
            var jobManager = TypeResolver.ResolveFromCache<IJobManager>();
            var job = jobManager.GetContextJob();

            if (job == null) return;
            
            Context.Site = Factory.GetSite(job.Options.SiteName);
        }

        public static void PutMessage(IMessage message)
        {
            var jobManager = TypeResolver.ResolveFromCache<IJobManager>();
            var job = jobManager.GetContextJob();
            if (job != null)
            {
                job.MessageQueue.PutMessage(message);
            }
            else
            {
                message.Execute();
            }
        }

        protected bool IsPowerShellScriptItem(Item scriptItem)
        {
            if (scriptItem.IsPowerShellScript()) return true;

            WriteError(typeof(CmdletInvocationException),
                Texts.General_Operation_failed_wrong_data_template,
                ErrorIds.InvalidItemType, ErrorCategory.InvalidArgument, HostData.ScriptingHost.SessionId);
            return false;
        }
    }
}