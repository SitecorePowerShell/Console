using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Rules;
using Sitecore.Rules.Conditions;
using Spe.Core.Settings;

namespace Spe.Core.Rules
{
    public class PowerShellDelegatedAccess<T> : WhenCondition<T> where T : RuleContext
    {
        protected override bool Execute(T ruleContext)
        {
            Assert.ArgumentNotNull(ruleContext, nameof(ruleContext));

            Item scriptItem;
            if (ruleContext.Parameters.TryGetValue("ScriptItem", out var item))
            {
                scriptItem = item as Item;
            }
            else
            {
                return false;
            }

            var currentUser = Context.User;

            return DelegatedAccessManager.IsElevated(currentUser, scriptItem);
        }
    }
}
