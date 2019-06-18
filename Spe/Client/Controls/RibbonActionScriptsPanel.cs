using System;
using System.Linq;
using System.Web.UI;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Rules;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Shell.Web.UI.WebControls;
using Sitecore.Web.UI.WebControls.Ribbons;
using Spe.Commands.Interactive;
using Spe.Core.Modules;
using Spe.Core.Settings;
using Spe.Core.Utility;
using Control = Sitecore.Web.UI.HtmlControls.Control;

namespace Spe.Client.Controls
{
    public class RibbonActionScriptsPanel : RibbonPanel
    {
        public override void Render(HtmlTextWriter output, Ribbon ribbon, Item button, CommandContext context)
        {
            var typeName = context.Parameters["type"];
            var viewName = context.Parameters["viewName"];
            var ruleContext = new RuleContext
            {
                Item = context.CustomData as Item
            };
            ruleContext.Parameters["ViewName"] = viewName;

            ShowListViewFeatures features;
            var showShared = Enum.TryParse(context.Parameters["features"] ?? "", out features) &&
                             features.HasFlag(ShowListViewFeatures.SharedActions);

            if (!string.IsNullOrEmpty(typeName))
            {
                foreach (
                    Item scriptItem in
                        ModuleManager.GetFeatureRoots(IntegrationPoints.ReportActionFeature)
                            .Select(parent => parent.Paths.GetSubItem(typeName))
                            .Where(scriptLibrary => scriptLibrary != null)
                            .SelectMany(scriptLibrary => scriptLibrary.Children)
                            .Where(
                                scriptItem =>
                                    RulesUtils.EvaluateRulesForView(scriptItem[FieldNames.ShowRule], ruleContext, !showShared)))
                {
                    RenderSmallButton(output, ribbon, Control.GetUniqueID("export"),
                        Translate.Text(scriptItem.DisplayName),
                        scriptItem["__Icon"], scriptItem.Appearance.ShortDescription,
                        $"listview:action(scriptDb={scriptItem.Database.Name},scriptID={scriptItem.ID})",
                        RulesUtils.EvaluateRules(scriptItem[FieldNames.EnableRule], ruleContext) &&
                        context.Parameters["ScriptRunning"] == "0",
                        false);
                }
            }
        }
    }
}