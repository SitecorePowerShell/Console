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
                            Click = "webedit:fieldeditor(fields={0}, command={{007A0E9E-59AA-48BB-84F2-6D25A8D2EF80}})",
                            Icon = scriptItem.Appearance.Icon,
                            Tooltip = scriptItem.Name,
                            Header = scriptItem.Name,
                            Type = "sticky" // sticky keeps it from being hidden in the 'more' dropdown
                        }
                    }, args);
                    /*
                                        using (var session = ScriptSessionManager.NewSession(ApplicationNames.Default, true))
                                        {
                                            session.SetVariable("pipelineArgs", args);

                                            try
                                            {
                                                session.SetItemLocationContext(args.Item);
                                                session.ExecuteScriptPart(scriptItem, false);
                                            }
                                            catch (Exception ex)
                                            {
                                                Log.Error(ex.Message, this);
                                            }
                                        }
                    */
                }
            }
        }
    }
}