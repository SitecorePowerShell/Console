using System;
using System.Linq;
using Sitecore.Diagnostics;
using Sitecore.Rules;
using Sitecore.Rules.Conditions;

namespace Spe.Core.Rules
{
    public class PowerShellExperienceButtonChromeName<T> : WhenCondition<T> where T : RuleContext
    {
        // Properties
        public string ChromeName { get; set; }
        // Methods
        protected override bool Execute(T ruleContext)
        {
            Assert.ArgumentNotNull(ruleContext, "ruleContext");

            if (string.IsNullOrEmpty(ChromeName))
            {
                return true;
            }

            if (!ruleContext.Parameters.ContainsKey("ChromeName"))
            {
                return false;
            }

            var currentChromeName = (ruleContext.Parameters["ChromeName"] ?? string.Empty).ToString();
            var chromeNames = ChromeName.Split('|');
            return chromeNames.Contains(currentChromeName, StringComparer.OrdinalIgnoreCase);
        }
    }
}