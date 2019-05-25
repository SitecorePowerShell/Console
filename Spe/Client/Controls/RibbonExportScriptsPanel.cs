using System;
using System.Linq;
using System.Web.UI;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Rules;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Shell.Web.UI.WebControls;
using Sitecore.Web.UI.WebControls.Ribbons;
using Spe.Commandlets.Interactive;
using Spe.Core.Modules;
using Spe.Core.Settings;
using Spe.Core.Utility;
using Control = Sitecore.Web.UI.HtmlControls.Control;

namespace Spe.Client.Controls
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