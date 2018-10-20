using System;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Rules;
using Sitecore.Rules.Conditions;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Core.Rules
{
    public class PowerShellUserRoleWithInheritanceCondition<T> : WhenCondition<T> where T : RuleContext
    {
        protected override bool Execute(T ruleContext)
        {
            Assert.ArgumentNotNull(ruleContext, nameof(ruleContext));
 
            var configuredRoles = this.Value;

            if (configuredRoles == null) return false;

            foreach (var configuredRole in configuredRoles.Split(new [] { "|" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var role = Role.FromName(configuredRole);
 
                if (RolesInRolesManager.IsUserInRole(Context.User, role, true))
                {
                    return true;
                }
            }

            return false;
        }
 
        public string Value { get; set; }
    }
}