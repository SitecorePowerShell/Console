using System;
using System.Globalization;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Client.Commands.MenuItems
{
    [Serializable]
    public class ExecutePowerShellScript : Command
    {
        private string scriptId;
        private string scriptDb;
        private string itemId;
        private string itemDb;
        private string itemLang;
        private string itemVer;

        public override CommandState QueryState(CommandContext context)
        {
            return context.Parameters["ScriptRunning"] != "1" ? CommandState.Enabled : CommandState.Disabled;
        }

        public override void Execute(CommandContext context)
        {
            Assert.ArgumentNotNull(context, "context");
            scriptId = context.Parameters["script"];
            scriptDb = context.Parameters["scriptDb"];
            if (context.Items.Length > 0)
            {
                var item = context.Items[0];
                itemId = item.ID.ToString();
                itemDb = item.Database.Name;
                itemLang = item.Language.Name;
                itemVer = item.Version.Number.ToString(CultureInfo.InvariantCulture);

            }
            SheerResponse.CheckModified(false);
            Context.ClientPage.Start(this, "Process");
        }

        protected void Process(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (args.IsPostBack)
            {
                Context.ClientPage.SendMessage(this, $"item:refresh(id={itemId})");
                Context.ClientPage.SendMessage(this, $"item:refreshchildren(id={itemId})");
            }
            else
            {
                var str = new UrlString(UIUtil.GetUri("control:PowerShellRunner"));
                if (!string.IsNullOrWhiteSpace(itemId))
                {
                    str.Append("id", itemId);
                    str.Append("db", itemDb);
                    str.Append("lang", itemLang);
                    str.Append("ver", itemVer);
                }
                str.Append("scriptId", scriptId);
                str.Append("scriptDb", scriptDb);
                //Context.ClientPage.ClientResponse.Broadcast(
                    SheerResponse.ShowModalDialog(str.ToString(), "400", "260", "PowerShell Script Results", true)
                //    ,"Shell")
                ;
                args.WaitForPostBack();
            }
        }
    }
}
