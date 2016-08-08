using System;
using System.Linq;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Modules;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Collections;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.GetChromeData;
using Sitecore.Rules;
using Sitecore.Text;

namespace Cognifide.PowerShell.Integrations.Pipelines
{
    public class PageEditorExperienceButtonScript : GetChromeDataProcessor
    {
        public string IntegrationPoint => IntegrationPoints.PageEditorExperienceButtonFeature;

        public override void Process(GetChromeDataArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            var page = Sitecore.Context.Item;
            if (page == null)
            {
                // a bit of a hacky way to determine the caller so we can have page context
                var parameters = new UrlString("?" + Context.Items["SC_FORM"]);
                var itemId = ID.Parse(parameters.Parameters["itemid"], args.Item.ID);
                var x = parameters.Parameters["xxx"];
                var language = string.IsNullOrWhiteSpace(parameters.Parameters["language"])
                    ? args.Item.Language
                    : LanguageManager.GetLanguage(parameters.Parameters["language"]);
                page = args.Item.Database.GetItem(itemId, language);
            }

            var chromeType = args.ChromeType;
            var chromeName = args.ChromeData.DisplayName;
            var click = "webedit:script(scriptId={0}, scriptdB={1})";

            args.CommandContext.Parameters["pageId"] = page.ID.ToString();
            args.CommandContext.Parameters["pageLang"] = page.Language.Name;
            args.CommandContext.Parameters["pageVer"] = page.Version.Number.ToString();
            args.CommandContext.Parameters["ChromeType"] = chromeType;
            args.CommandContext.Parameters["ChromeName"] = chromeName;
            var ruleContext = new RuleContext
            {
                Item = args.Item
            };

            foreach (var parameter in args.CommandContext.Parameters.AllKeys)
            {
                ruleContext.Parameters[parameter] = args.CommandContext.Parameters[parameter];
            }

            foreach (var libraryItem in ModuleManager.GetFeatureRoots(IntegrationPoint))
            {
                if (!libraryItem.HasChildren) return;

                AddButton(args, libraryItem, ruleContext, click);
            }
        }

        private void AddButton(GetChromeDataArgs args, Item libraryItem, RuleContext ruleContext, string click)
        {
            foreach (var scriptItem in libraryItem.Children.ToList())
            {
                if (!RulesUtils.EvaluateRules(scriptItem["ShowRule"], ruleContext))
                {
                    continue;
                }

                if (scriptItem.IsPowerShellLibrary())
                {
                    AddButton(args,scriptItem,ruleContext,click);
                    continue;
                }

                if (scriptItem.IsPowerShellScript())
                {
                    AddButtonsToChromeData(new[]
                    {
                        new WebEditButton
                        {
                            Click = string.Format(click, scriptItem.ID, scriptItem.Database.Name),
                            Icon = scriptItem.Appearance.Icon,
                            Tooltip = scriptItem.Name,
                            Header = scriptItem.Name,
                            Type = "sticky", // sticky keeps it from being hidden in the 'more' dropdown
                        }
                    }, args);
                }
            }
        }
    }
}