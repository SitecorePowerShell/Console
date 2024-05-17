using System;
using System.Globalization;
using Sitecore;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using Sitecore.StringExtensions;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using Spe.Core.Extensions;

namespace Spe.Client.Commands.MenuItems
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
            else
            {
                //Found this to occur with the report menu.
                itemId = null;
                itemDb = null;
                itemLang = null;
                itemVer = null;
            }
            
            itemId =  context.Parameters.TryGetValue(NameValueCollectionExtensions.ItemId, itemId);
            itemDb = context.Parameters.TryGetValue(NameValueCollectionExtensions.ItemDb, itemDb);
            itemLang = context.Parameters.TryGetValue(NameValueCollectionExtensions.ItemLang, itemLang);
            itemVer = context.Parameters.TryGetValue(NameValueCollectionExtensions.ItemVer, itemVer);
            scriptId = context.Parameters.TryGetValue(NameValueCollectionExtensions.ScriptId, itemVer);
            

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
                if (!args.HasResult || args.Result.IsNullOrEmpty()) return;

                foreach (var closeMessage in args.Result.Split('\n'))
                {
                    Context.ClientPage.ClientResponse.Timer(closeMessage, 2);
                }
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
                    if ((bool) (args.Properties["UsesBrowserWindows"] ?? false))
                    {
                        str.Append("cfs", "1");
                    }
                }
                str.Append("scriptId", scriptId);
                str.Append("scriptDb", scriptDb);
                SheerResponse.ShowModalDialog(str.ToString(), "400", "260", "", true);
                args.WaitForPostBack();
            }
        }
    }
}
