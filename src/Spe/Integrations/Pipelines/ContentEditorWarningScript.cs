using System;
using System.Linq;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.GetContentEditorWarnings;
using Sitecore.Rules;
using Spe.Core.Diagnostics;
using Spe.Core.Extensions;
using Spe.Core.Host;
using Spe.Core.Modules;
using Spe.Core.Settings;
using Spe.Core.Utility;

namespace Spe.Integrations.Pipelines
{
    public class ContentEditorWarningScript
    {
        public string IntegrationPoint
        {
            get { return IntegrationPoints.ContentEditorWarningFeature; }
        }

        public void Process(GetContentEditorWarningsArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            RuleContext GetRuleContext(Item contextItem, Item scriptItem)
            {
                var ruleContext = new RuleContext
                {
                    Item = contextItem ?? scriptItem
                };
                ruleContext.Parameters.Add("ScriptItem", scriptItem);

                return ruleContext;
            }            

            Func<Item, bool> filter = si => si.IsPowerShellScript()
                                            && !string.IsNullOrWhiteSpace(si[Templates.Script.Fields.ScriptBody])
                                            && RulesUtils.EvaluateRules(si[Templates.Script.Fields.EnableRule], GetRuleContext(args.Item, si));

            foreach (var libraryItem in ModuleManager.GetFeatureRoots(IntegrationPoint))
            {
                if (!RulesUtils.EvaluateRules(libraryItem?[FieldNames.EnableRule], GetRuleContext(args.Item, libraryItem)))
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
                            PowerShellLog.Error($"Error while invoking script '{scriptItem?.Paths.Path}' in Content Editor Warning pipeline.", ex);
                        }
                    }
                }
            }
        }
    }
}