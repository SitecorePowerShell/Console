using System;
using System.Linq;
using System.Web.UI;
using Cognifide.PowerShell.Commandlets.Interactive;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Modules;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Rules;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Shell.Web.UI.WebControls;
using Sitecore.Web.UI.WebControls.Ribbons;
using Control = Sitecore.Web.UI.HtmlControls.Control;

namespace Cognifide.PowerShell.Client.Controls
{
    public class RibbonExportScriptsPanel : RibbonPanel
    {
        public override void Render(HtmlTextWriter output, Ribbon ribbon, Item button, CommandContext context)
        {
            var viewName = context.Parameters["viewName"];
            var ruleContext = new RuleContext
            {
                Item = context.CustomData as Item
            };
            ruleContext.Parameters["ViewName"] = viewName;

            ShowListViewFeatures features;
            var showShared = Enum.TryParse(context.Parameters["features"] ?? "", out features) &&
                             features.HasFlag(ShowListViewFeatures.SharedExport);

            foreach (
                Item scriptItem in
                    ModuleManager.GetFeatureRoots(IntegrationPoints.ListViewExportFeature)
                        .SelectMany(parent => parent.Children)
                        .Where(scriptItem => RulesUtils.EvaluateRulesForView(scriptItem[FieldNames.ShowRule], ruleContext, !showShared)))
            {
                RenderSmallButton(output, ribbon, Control.GetUniqueID("export"),
                    Translate.Text(scriptItem.DisplayName),
                    scriptItem[Sitecore.FieldIDs.Icon], scriptItem.Appearance.ShortDescription,
                    string.Format("export:results(scriptDb={0},scriptID={1})", scriptItem.Database.Name,
                        scriptItem.ID),
                    RulesUtils.EvaluateRules(scriptItem[FieldNames.EnableRule], context.CustomData as Item) &&
                    context.Parameters["ScriptRunning"] == "0",
                    false);
            }
        }
    }
}