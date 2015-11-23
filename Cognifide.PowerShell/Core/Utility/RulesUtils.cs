using System;
using System.Linq;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Settings;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Rules;

namespace Cognifide.PowerShell.Core.Utility
{
    public static class RulesUtils
    {
        public static bool EvaluateRules(string strRules, Item contextItem, bool failEmpty = false)
        {
            if (string.IsNullOrEmpty(strRules) || strRules.Length < 70)
            {
                return !failEmpty;
            }
            // hacking the rules xml
            var rules = RuleFactory.ParseRules<RuleContext>(Factory.GetDatabase(ApplicationSettings.RulesDb), strRules);
            var ruleContext = new RuleContext
            {
                Item = contextItem
            };

            return !rules.Rules.Any() || rules.Rules.Any(rule => rule.Evaluate(ruleContext));
        }

        public static bool EvaluateRules(string strRules, RuleContext ruleContext, bool failEmpty = false)
        {
            if (string.IsNullOrEmpty(strRules) || strRules.Length < 70)
            {
                return !failEmpty;
            }
            // hacking the rules xml
            var rules = RuleFactory.ParseRules<RuleContext>(Factory.GetDatabase(ApplicationSettings.RulesDb), strRules);
            return !rules.Rules.Any() || rules.Rules.Any(rule => rule.Evaluate(ruleContext));
        }
        public static bool EvaluateRulesForView(string strRules, RuleContext ruleContext, bool failNonSpecific = false)
        {
            if (string.IsNullOrEmpty(strRules) || strRules.Length < 70)
            {
                return !failNonSpecific;
            }
            var viewName = ruleContext.Parameters["ViewName"];
            if (failNonSpecific && 
                (!"ValidViewName".IsSubstringOf(strRules) || !$"\"{viewName}\"".IsSubstringOf(strRules)))
            {
                return false;
            }

            var rules = RuleFactory.ParseRules<RuleContext>(Factory.GetDatabase(ApplicationSettings.RulesDb), strRules);
            return !rules.Rules.Any() || rules.Rules.Any(rule => rule.Evaluate(ruleContext));
        }
    }
}