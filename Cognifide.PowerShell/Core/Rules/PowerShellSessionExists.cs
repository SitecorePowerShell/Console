using Cognifide.PowerShell.Core.Host;
using Sitecore.Diagnostics;
using Sitecore.Rules;
using Sitecore.Rules.Conditions;

namespace Cognifide.PowerShell.Core.Rules
{
    public class PowerShellSessionExists<T> : WhenCondition<T> where T : RuleContext
    {
        // Properties
        public string PersistentSessionId { get; set; }
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
    }
}