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
        [Parameter]
        public User User { get; set; }

        protected override void BeginProcessing()
        {
            if (User == null)
            {
                User = Context.User;
            }
        }

        protected override void ProcessItem(Item item)
        {
            using (new SecurityStateSwitcher(SecurityState.Enabled))
            {
                if (User.Name.Is(Context.User.Name))
                {
                    ProcessItemInUserContext(item);
                }
                else
                {
                    using (new UserSwitcher(User))
                    {
                        ProcessItemInUserContext(item);
                    }
                }
            }
        }

        protected abstract void ProcessItemInUserContext(Item item);
    }
}