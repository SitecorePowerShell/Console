using System.Linq;
using Cognifide.PowerShell.Core.Settings;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Rules;

namespace Cognifide.PowerShell.Core.Utility
{
    public static class RulesUtils
    {
        public static bool EvaluateRules(string strRules, Item contextItem)
        {
            if (string.IsNullOrEmpty(strRules) || strRules.Length < 20)
            {
                return true;
            }
            // hacking the rules xml
            var rules = RuleFactory.ParseRules<RuleContext>(Factory.GetDatabase(ApplicationSettings.RulesDb), strRules);
            var ruleContext = new RuleContext
            {
                Item = contextItem
            };

            return !rules.Rules.Any() || rules.Rules.Any(rule => rule.Evaluate(ruleContext));
        }

        public static bool EvaluateRules(string strRules, Item item, string viewName)
        {
            if (string.IsNullOrEmpty(strRules) || strRules.Length < 20)
            {
                return true;
            }
            // hacking the rules xml
            var rules = RuleFactory.ParseRules<RuleContext>(Factory.GetDatabase(ApplicationSettings.RulesDb), strRules);
            var ruleContext = new RuleContext
            {
                Item = item
            };
            ruleContext.Parameters["ViewName"] = viewName;
            return !rules.Rules.Any() || rules.Rules.Any(rule => rule.Evaluate(ruleContext));
        }
    }
}