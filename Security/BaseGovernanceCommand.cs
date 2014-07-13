using System.Management.Automation;
using Cognifide.PowerShell.Extensions;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Security.Accounts;
using Sitecore.SecurityModel;

namespace Cognifide.PowerShell.Security
{
    public abstract class BaseGovernanceCommand : BaseItemCommand
    {
        [Alias("User")]
        [Parameter]
        public AccountIdentity Identity { get; set; }

        protected override void BeginProcessing()
        {
            if (Identity == null)
            {
                Identity = new AccountIdentity(Context.User);
            }
        }

        protected override void ProcessItem(Item item)
        {
            using (new SecurityStateSwitcher(SecurityState.Enabled))
            {
                if (Identity.Name.Is(Context.User.Name))
                {
                    ProcessItemInUserContext(item);
                }
                else
                {
                    using (new UserSwitcher(Identity.Name, false))
                    {
                        ProcessItemInUserContext(item);
                    }
                }
            }
        }

        protected abstract void ProcessItemInUserContext(Item item);
    }
}