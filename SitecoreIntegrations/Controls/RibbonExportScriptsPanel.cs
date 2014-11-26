using System.Linq;
using System.Web.UI;
using Cognifide.PowerShell.PowerShellIntegrations;
using Cognifide.PowerShell.PowerShellIntegrations.Modules;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Cognifide.PowerShell.Utility;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Rules;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Shell.Web.UI.WebControls;
using Sitecore.Web.UI.WebControls.Ribbons;
using Control = Sitecore.Web.UI.HtmlControls.Control;

namespace Cognifide.PowerShell.SitecoreIntegrations.Controls
{
    public class RibbonExportScriptsPanel : RibbonPanel
    {
        public override void Render(HtmlTextWriter output, Ribbon ribbon, Item button, CommandContext context)
        {

            foreach (Item parent in ModuleManager.GetFeatureRoots(IntegrationPoints.ListViewExportFeature))
            {
                foreach (Item scriptItem in parent.Children)
                {
                    if (!RulesUtils.EvaluateRules(scriptItem["ShowRule"], context.CustomData as Item))
                    {
                        continue;
                    }
                    RenderSmallButton(output, ribbon, Control.GetUniqueID("export"),
                        Translate.Text(scriptItem.DisplayName),
                        scriptItem["__Icon"], string.Empty,
                        string.Format("export:results(scriptDb={0},scriptID={1})", scriptItem.Database.Name,
                            scriptItem.ID),
                        RulesUtils.EvaluateRules(scriptItem["EnableRule"], context.CustomData as Item) &&
                        context.Parameters["ScriptRunning"] == "0",
                        false);
                }
            }
        }
    }
}