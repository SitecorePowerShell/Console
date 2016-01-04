using System;
using System.Linq;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Modules;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Fields;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.GetChromeData;
using Sitecore.Rules;

namespace Cognifide.PowerShell.Integrations.Pipelines
{
    public class PageEditorExperienceButtonScript : GetChromeDataProcessor
    {
        public string IntegrationPoint => IntegrationPoints.PageEditorExperienceButtonFeature;

        public override void Process(GetChromeDataArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            var page = Sitecore.Context.Item;
            var chromeType = args.ChromeType;
            var chromeName = args.ChromeData.DisplayName;

            var ruleContext = new RuleContext
            {
                Item = args.Item
            };

            foreach (var parameter in args.CommandContext.Parameters.AllKeys)
            {
                ruleContext.Parameters[parameter] = args.CommandContext.Parameters[parameter];
            }
            ruleContext.Parameters["ChromeType"] = chromeType;
            ruleContext.Parameters["ChromeName"] = chromeName;

            foreach (var libraryItem in ModuleManager.GetFeatureRoots(IntegrationPoint))
            {
                if (!libraryItem.HasChildren) return;

                foreach (var scriptItem in libraryItem.Children.ToList())
                {
                    if (!RulesUtils.EvaluateRules(scriptItem["ShowRule"], ruleContext))
                    {
                        continue;
                    }
                    AddButtonsToChromeData(new[]
                    {
                        new WebEditButton
                        {
                            Click = $"webedit:script(scriptId={scriptItem.ID}, scriptdB={scriptItem.Database.Name}, "+
                                    $"pageId={page.ID}, pageLang={page.Language.Name}, pageVer={page.Version.Number},"+
                                    $"chromeType={chromeType}, chromeName={chromeName})",
                            Icon = scriptItem.Appearance.Icon, 
                            Tooltip = scriptItem.Name,
                            Header = scriptItem.Name,
                            Type = "sticky" // sticky keeps it from being hidden in the 'more' dropdown
                        }
                    }, args);
                }
            }
        }
    }
}