using System;
using System.Linq;
using Sitecore.Diagnostics;
using Sitecore.Rules;
using Sitecore.Rules.Conditions;

namespace Cognifide.PowerShell.Core.Rules
{
    public class PowerShellListViewNameMatch<T> : WhenCondition<T> where T : RuleContext
    {
        // Properties
        public string ValidViewName { get; set; }
        // Methods
        protected override bool Execute(T ruleContext)
        {
            Assert.ArgumentNotNull(ruleContext, "ruleContext");

            if (string.IsNullOrEmpty(ValidViewName))
            {
                return true;
            }
            if (!ruleContext.Parameters.ContainsKey("ViewName"))
            {
                return false;
            }

            var currentViewName = (ruleContext.Parameters["ViewName"] ?? string.Empty).ToString();
            var viewNames = ValidViewName.Split('|');
            return viewNames.Contains(currentViewName, StringComparer.OrdinalIgnoreCase);
        }
    }
}