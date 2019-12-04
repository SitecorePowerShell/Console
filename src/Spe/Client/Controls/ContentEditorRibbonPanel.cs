using System.Web.UI;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Shell.Web.UI.WebControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls.Ribbons;
using Spe.Core.Extensions;
using Spe.Core.Modules;
using Spe.Core.Settings.Authorization;
using Spe.Core.Utility;
using Control = Sitecore.Web.UI.HtmlControls.Control;

namespace Spe.Client.Controls
{
    public class ContentEditorRibbonPanel : RibbonPanel
    {
        public override void Render(HtmlTextWriter output, Ribbon ribbon, Item button, CommandContext context)
        {
            if (!ServiceAuthorizationManager.IsUserAuthorized(WebServiceSettings.ServiceExecution, Context.User.Name))
            {
                return;
            }
            var psButtons = button.GetChildren();
            foreach (Item psButton in psButtons)
            {
                var command = (psButton.Fields["Click"] ?? psButton.Fields["Command"]).Value;
                var msg = Message.Parse(this, command);
                var scriptDb = msg.Arguments["scriptDB"];
                var scriptId = msg.Arguments["script"];

                if (string.IsNullOrWhiteSpace(scriptDb) || string.IsNullOrWhiteSpace(scriptId))
                {
                    continue;
                }

                var scriptItem = Factory.GetDatabase(scriptDb).GetItem(scriptId);

                if (!scriptItem.IsPowerShellScript() ||
                    !RulesUtils.EvaluateRules(scriptItem[Templates.Script.Fields.ShowRule], context.Items[0]))
                {
                    continue;
                }

                var featureRoot = ModuleManager.GetItemModule(scriptItem)?
                    .GetFeatureRoot(IntegrationPoints.ContentEditorRibbonFeature);
                if (!RulesUtils.EvaluateRules(featureRoot?[Templates.ScriptLibrary.Fields.ShowRule], context.Items[0])) continue;

                RenderButton(output, psButton.TemplateID, ribbon, Control.GetUniqueID("script"),
                    Translate.Text(psButton.DisplayName), scriptItem["__Icon"], scriptItem[TemplateFieldIDs.ToolTip],
                    command, RulesUtils.EvaluateRules(scriptItem[Templates.Script.Fields.EnableRule], context.Items[0]), context,
                    psButton.Paths.Path);
            }
        }

        public void RenderButton(HtmlTextWriter output, ID buttonTemplateId, Ribbon ribbon, string id, string header, string icon,
            string tooltip, string command, bool enabled, CommandContext context, string menuPath)
        {
            var buttonId = Control.GetUniqueID("script");
            if (buttonTemplateId == Ribbon.LargeButton)
            {
                var button = new LargeButton
                {
                    ID = buttonId,
                    Header = header,
                    Icon = icon,
                    Enabled = enabled,
                    ToolTip = tooltip,
                    Command = GetClick(command, context, buttonId),
                };
                button.RenderControl(output);
            }
            else if (buttonTemplateId == Ribbon.LargeMenuComboButton)
            {
                var button = new LargeMenuComboButton
                {
                    ID = buttonId,
                    Header = header,
                    Icon = icon,
                    Enabled = enabled,
                    ToolTip = tooltip,
                    Command = GetClick(command, context, buttonId),
                    CommandContext = context,
                    Menu = menuPath
                };
                button.RenderControl(output);
            }
            else if (buttonTemplateId == Ribbon.SmallButton)
            {
                var button = new SmallButton
                {
                    ID = buttonId,
                    Header = header,
                    Icon = icon,
                    Enabled = enabled,
                    ToolTip = tooltip,
                    Command = GetClick(command, context, buttonId),
                };
                ribbon.RenderSmallButton(output, button);
            }
            else if (buttonTemplateId == Ribbon.SmallMenuComboButton)
            {
                var button = new SmallMenuComboButton
                {
                    ID = buttonId,
                    Header = header,
                    Icon = icon,
                    Enabled = enabled,
                    ToolTip = tooltip,
                    Command = GetClick(command, context, buttonId),
                    CommandContext = context,
                    Menu = menuPath
                };
                ribbon.RenderSmallButton(output, button);
            }
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