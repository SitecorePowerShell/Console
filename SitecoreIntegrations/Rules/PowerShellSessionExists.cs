using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Sitecore.Diagnostics;
using Sitecore.Rules;
using Sitecore.Rules.Conditions;

namespace Cognifide.PowerShell.SitecoreIntegrations.Rules
{
    public class PowerShellSessionExists<T> : WhenCondition<T> where T : RuleContext
    {
        // Methods
        protected override bool Execute(T ruleContext)
        {
            Assert.ArgumentNotNull(ruleContext, "ruleContext");

            if (string.IsNullOrEmpty(PersistentSessionId))
            {
                return true;
            }
            return ScriptSessionManager.SessionExists(PersistentSessionId);
        }

        // Properties
        public string PersistentSessionId { get; set; }
    }
}