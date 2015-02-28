using System.Web.UI;
using Cognifide.PowerShell.Core.Modules;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Shell.Web.UI.WebControls;
using Sitecore.Web.UI.WebControls.Ribbons;
using Control = Sitecore.Web.UI.HtmlControls.Control;

namespace Cognifide.PowerShell.Client.Controls
{
    public class RibbonActionScriptsPanel : RibbonPanel
    {
        public override void Render(HtmlTextWriter output, Ribbon ribbon, Item button, CommandContext context)
        {
            var typeName = context.Parameters["type"];
            var viewName = context.Parameters["viewName"];
            if (!string.IsNullOrEmpty(typeName))
            {
                foreach (var parent in ModuleManager.GetFeatureRoots(IntegrationPoints.ListViewRibbonFeature))
                {
                    var scriptLibrary = parent.Paths.GetSubItem(typeName);

                    if (scriptLibrary != null)
                    {
                        foreach (Item scriptItem in scriptLibrary.Children)
                        {
                            if (!RulesUtils.EvaluateRules(scriptItem["ShowRule"], context.CustomData as Item, viewName))
                            {
                                continue;
                            }
                            RenderSmallButton(output, ribbon, Control.GetUniqueID("export"),
                                Translate.Text(scriptItem.DisplayName),
                                scriptItem["__Icon"], string.Empty,
                                string.Format("listview:action(scriptDb={0},scriptID={1})", scriptItem.Database.Name,
                                    scriptItem.ID),
                                RulesUtils.EvaluateRules(scriptItem["EnableRule"], context.CustomData as Item, viewName) &&
                                context.Parameters["ScriptRunning"] == "0",
                                false);
                        }
                    }
                }
            }
        }
    }
}