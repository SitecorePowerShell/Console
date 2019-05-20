using System;
using Cognifide.PowerShell.Abstractions.VersionDecoupling.Interfaces;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Sitecore;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Core.Settings.Authorization
{
    [Serializable]
    public class ElevateSessionState : Command
    {
        public override CommandState QueryState(CommandContext context)
        {
            return CommandState.Enabled;
        }

        public override void Execute(CommandContext context)
        {
            var args = new ClientPipelineArgs {Parameters = {["itemId"] = context.Items[0].ID.ToString()}};
            Context.ClientPage.Start(this, nameof(SessionElevationPipeline), args);
        }
        public void SessionElevationPipeline(ClientPipelineArgs args)
        {
            if (!args.IsPostBack)
            {
                var url = new UrlString(UIUtil.GetUri("control:PowerShellSessionElevation"));
                url.Parameters["app"] = ApplicationNames.ItemSave;
                url.Parameters["action"] = SessionElevationManager.SaveAction;
                TypeResolver.Resolve<ISessionElevationWindowLauncher>().ShowSessionElevationWindow(url);
                args.WaitForPostBack(true);
            }
            else
            {
                Context.ClientPage.SendMessage(this, $"item:refresh(id={args.Properties["itemId"]})");
                //UpdateRibbon(args.Parameters["message"]);
            }
        }

    }
}