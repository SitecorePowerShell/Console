using System.Collections.Generic;
using System.Web.UI;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Rules;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Shell.Web.UI.WebControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls.Ribbons;
using Control = Sitecore.Web.UI.HtmlControls.Control;

namespace Cognifide.PowerShell.Client.Controls
{
    public class IsePluginPanel : RibbonPanel
    {
        public override void Render(HtmlTextWriter output, Ribbon ribbon, Item button, CommandContext context)
        {
            var contextChunks = context.CustomData as List<Item>;
            if (contextChunks != null)
            {
                var chunk = contextChunks[0];
                contextChunks.RemoveAt(0);
                var psButtons = chunk.Children;
                var contextItem = context.Items.Length > 0 ? context.Items[0] : null;

                var ruleContext = new RuleContext
                {
                    Item = contextItem
                };
                foreach (var parameter in context.Parameters.AllKeys)
                {
                    ruleContext.Parameters[parameter] = context.Parameters[parameter];
                }

                foreach (Item psButton in psButtons)
                {
                    if (!RulesUtils.EvaluateRules(psButton["ShowRule"], ruleContext))
                    {
                        continue;
                    }

                    RenderLargeButton(output, ribbon, Control.GetUniqueID("script"),
                        Translate.Text(psButton.DisplayName),
                        psButton["__Icon"], string.Empty,
                        $"ise:runplugin(scriptDb={psButton.Database.Name},scriptId={psButton.ID})",
                        context.Parameters["ScriptRunning"] == "0" && RulesUtils.EvaluateRules(psButton["EnableRule"], ruleContext),
                        false, context);
                }
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