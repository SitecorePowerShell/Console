﻿using System;
using System.Linq;
using System.Web.UI;
using Cognifide.PowerShell.Commandlets.Interactive;
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
                        ModuleManager.GetFeatureRoots(IntegrationPoints.ListViewRibbonFeature)
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