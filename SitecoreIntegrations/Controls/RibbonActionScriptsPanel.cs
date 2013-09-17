using System.Linq;
using System.Web.UI;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Rules;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Shell.Web.UI.WebControls;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.WebControls.Ribbons;
using Control = Sitecore.Web.UI.HtmlControls.Control;

namespace Cognifide.PowerShell.SitecoreIntegrations.Controls
{
    public class RibbonActionScriptsPanel : RibbonPanel
    {
        public override void Render(HtmlTextWriter output, Ribbon ribbon, Item button, CommandContext context)
        {
            string typeName = context.Parameters["type"];
            if (!string.IsNullOrEmpty(typeName))
            {
                Item scriptLibrary =
                    Factory.GetDatabase("master")
                        .GetItem("/sitecore/system/Modules/PowerShell/Script Library/Internal/List View/Ribbon/"+typeName);
                if (scriptLibrary != null)
                {
                    foreach (Item scriptItem in scriptLibrary.Children)
                    {
                        if (!EvaluateRules(scriptItem["ShowRule"], context.CustomData as Item))
                        {
                            continue;
                        }
                        RenderSmallButton(output, ribbon, Control.GetUniqueID("export"),
                            Translate.Text(scriptItem.DisplayName),
                            scriptItem["__Icon"], string.Empty,
                            string.Format("listview:action(scriptDb={0},scriptID={1})", scriptItem.Database.Name,
                                scriptItem.ID),
                            EvaluateRules(scriptItem["EnableRule"], context.CustomData as Item), false);
                    }
                }
            }
        }
        public static bool EvaluateRules(string strRules, Item item)
        {
            if (string.IsNullOrEmpty(strRules) || strRules.Length < 20)
            {
                return true;
            }
            // hacking the rules xml
            var rules = RuleFactory.ParseRules<RuleContext>(Factory.GetDatabase("master"), strRules);
            var ruleContext = new RuleContext
            {
                Item = item
            };

            return !rules.Rules.Any() || rules.Rules.Any(rule => rule.Evaluate(ruleContext));
        }

    }
}