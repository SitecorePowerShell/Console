using System;
using System.Collections.Specialized;
using System.Linq;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using Sitecore.StringExtensions;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using Spe.Core.Diagnostics;
using Spe.Core.Host;

namespace Spe.Client.Commands
{
    /// <summary>
    ///     Posted once per page load by SpeJobRecovery.js from the top window.
    ///     Claims the current user's interactive Runner jobs this tab should take
    ///     over (its own jobs after a reload or discard, anyone's abandoned ones)
    ///     and reopens a re-attached Runner for each, one after another.
    /// </summary>
    [Serializable]
    public class ClaimOrphanedJobsCommand : Command
    {
        private const string RetryDelayMs = "2000";

        public override void Execute(CommandContext context)
        {
            Assert.ArgumentNotNull(context, "context");
            var user = Context.User?.Name;
            if (string.IsNullOrEmpty(user) || !Context.User.IsAuthenticated) return;

            var claimed = InteractiveJobRegistry.ClaimOrphans(user, InteractiveJobRegistry.CurrentTabId,
                origin => origin != InteractiveJobOrigin.Ise, out var ownTabStillFresh);

            if (claimed.Count == 0)
            {
                if (ownTabStillFresh)
                {
                    // The page we replace polled moments ago; ask again once its
                    // lease has gone quiet for OwnTabReclaimSeconds.
                    var attempt = MainUtil.GetInt(context.Parameters["attempt"], 0);
                    SheerResponse.Eval(
                        $"if(window.speJobRecovery){{speJobRecovery.retryDiscovery({RetryDelayMs},{attempt});}}");
                }

                return;
            }

            var parameters = new NameValueCollection { ["index"] = "0", ["count"] = claimed.Count.ToString() };
            for (var i = 0; i < claimed.Count; i++)
            {
                parameters["handle" + i] = claimed[i].Handle;
                parameters["claim" + i] = claimed[i].LeaseToken;
                PowerShellLog.Info(
                    $"[Runner] action=reattachClaimed user={user} job={claimed[i].Handle} origin={claimed[i].Origin}");
            }

            Context.ClientPage.Start(this, nameof(Run), parameters);
        }

        protected void Run(ClientPipelineArgs args)
        {
            var index = MainUtil.GetInt(args.Parameters["index"], 0);
            if (args.IsPostBack)
            {
                if (args.HasResult && !args.Result.IsNullOrEmpty())
                {
                    ScriptExecutionResult.Parse(args.Result).ExecuteResults();
                }

                index++;
                args.Parameters["index"] = index.ToString();
            }

            if (index >= MainUtil.GetInt(args.Parameters["count"], 0)) return;

            var url = new UrlString(UIUtil.GetUri("control:PowerShellRunner"));
            url.Append("reattach", args.Parameters["handle" + index]);
            url.Append("claim", args.Parameters["claim" + index]);
            SheerResponse.ShowModalDialog(url.ToString(), "400", "260", string.Empty, true);
            args.WaitForPostBack();
        }
    }
}
