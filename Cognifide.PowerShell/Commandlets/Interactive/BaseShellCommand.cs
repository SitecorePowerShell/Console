using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Settings.Authorization;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data.Items;
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

        protected bool IsPowerShellScriptItem(Item scriptItem)
        {
            if (!scriptItem.IsPowerShellScript())
            {
                WriteError(typeof(CmdletInvocationException),
                    SessionElevationErrors.MessageOperationFailedWrongDataTemplate,
                    ErrorIds.InvalidItemType, ErrorCategory.InvalidArgument, HostData.ScriptingHost.SessionId);
                return false;
            }
            return true;
        }
    }
}