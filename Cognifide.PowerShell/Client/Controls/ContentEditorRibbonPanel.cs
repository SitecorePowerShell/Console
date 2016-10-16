using System.Web.UI;
using Cognifide.PowerShell.Core.Settings.Authorization;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Shell.Web.UI.WebControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls.Ribbons;
using Control = Sitecore.Web.UI.HtmlControls.Control;

namespace Cognifide.PowerShell.Client.Controls
{
    public class ContentEditorRibbonPanel : RibbonPanel
    {
        public override void Render(HtmlTextWriter output, Ribbon ribbon, Item button, CommandContext context)
        {
            if (!ServiceAuthorizationManager.IsUserAuthorized(WebServiceSettings.ServiceExecution, Context.User.Name,
                false))
            {
                return;
            }
            var psButtons = button.GetChildren();
            foreach (Item psButton in psButtons)
            {
                var msg = Message.Parse(this, psButton["Click"]);
                var scriptDb = msg.Arguments["scriptDB"];
                var scriptId = msg.Arguments["script"];

                if (string.IsNullOrWhiteSpace(scriptDb) || string.IsNullOrWhiteSpace(scriptId))
                {
                    continue;
                }

                var scriptItem = Factory.GetDatabase(scriptDb).GetItem(scriptId);
                if (scriptItem == null || !RulesUtils.EvaluateRules(scriptItem["ShowRule"], context.Items[0]))
                {
                    continue;
                }

                RenderLargeButton(output, ribbon, Control.GetUniqueID("script"),
                    Translate.Text(scriptItem.DisplayName),
                    scriptItem["__Icon"], string.Empty,
                    psButton["Click"],
                    RulesUtils.EvaluateRules(scriptItem["EnableRule"], context.Items[0]),
                    false, context);

                return;
            }
        }

        public void RenderLargeButton(HtmlTextWriter output, Ribbon ribbon, string id, string header, string icon,
            string tooltip, string command, bool enabled, bool down, CommandContext context)
        {
            var buttonId = Control.GetUniqueID("script");
            var largeButton = new LargeButton
            {
                ID = buttonId,
                Header = header,
                Icon = icon,
                Down = down,
                Enabled = enabled,
                ToolTip = tooltip,
                Command = GetClick(command, context, buttonId),
            };

            largeButton.RenderControl(output);
        }

        private static string GetClick(string click, CommandContext commandContext, string buttonId)
        {
            Assert.ArgumentNotNull(click, "click");
            var itemArray = (commandContext == null) ? new Item[0] : commandContext.Items;
            if (itemArray.Length == 1)
            {
                Assert.IsNotNull(commandContext, "context");
                click = click.Replace("$Target", buttonId);
                click = click.Replace("$ItemID", itemArray[0].ID.ToString());
            }
            return click;
        }
    }
}