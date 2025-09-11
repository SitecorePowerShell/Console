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
using Spe.Core.Extensions;
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

            var showShared = Enum.TryParse(context.Parameters["features"] ?? "", out ShowListViewFeatures features) &&
                             features.HasFlag(ShowListViewFeatures.SharedActions);

            if (string.IsNullOrEmpty(typeName)) return;

            RuleContext GetRuleContext(Item contextItem, Item scriptItem)
            {
                var ruleContext = new RuleContext
                {
                    Item = context.CustomData as Item
                };
                ruleContext.Parameters["ViewName"] = viewName;
                ruleContext.Parameters.Add("ScriptItem", scriptItem);

                return ruleContext;
            }

            Func<Item, bool> filter = si => si.IsPowerShellScript()
                                            && !string.IsNullOrWhiteSpace(si[Templates.Script.Fields.ScriptBody])
                                            && RulesUtils.EvaluateRulesForView(si[Templates.Script.Fields.ShowRule], GetRuleContext(context.CustomData as Item, si));

            foreach (var libraryItem in ModuleManager.GetFeatureRoots(IntegrationPoints.ReportActionFeature)
                .Select(parent => parent.Paths.GetSubItem(typeName)).Where(scriptLibrary => scriptLibrary != null))
            {
                if (!RulesUtils.EvaluateRulesForView(libraryItem?[FieldNames.ShowRule], GetRuleContext(context.CustomData as Item, libraryItem)))
                {
                    continue;
                }

                var applicableScriptItems = libraryItem?.Children?.Where(filter).ToArray();
                if (applicableScriptItems == null || !applicableScriptItems.Any())
                {
                    continue;
                }

                foreach (var scriptItem in applicableScriptItems)
                {
                    RenderSmallButton(output, ribbon, Control.GetUniqueID("export"),
                    Translate.Text(scriptItem.DisplayName),
                    scriptItem["__Icon"], scriptItem.Appearance.ShortDescription,
                    $"listview:action(scriptDb={scriptItem.Database.Name},scriptID={scriptItem.ID})",
                    RulesUtils.EvaluateRules(scriptItem[FieldNames.EnableRule], GetRuleContext(context.CustomData as Item, scriptItem)) &&
                    context.Parameters["ScriptRunning"] == "0",
                    false);
                }
            }
        }
    }
}