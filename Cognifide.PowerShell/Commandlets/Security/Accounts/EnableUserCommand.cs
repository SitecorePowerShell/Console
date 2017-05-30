using System.Management.Automation;
using System.Web.Security;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Validation;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Commandlets.Security.Accounts
{
    [Cmdlet(VerbsLifecycle.Enable, "User", DefaultParameterSetName = "Id", SupportsShouldProcess = true)]
    public class EnableUserCommand : BaseSecurityCommand
    {
        [Alias("Name")]
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0,
            ParameterSetName = "Id")]
        [ValidateNotNullOrEmpty]
        [AutocompleteSet(nameof(RoleNames))]
        public AccountIdentity Identity { get; set; }

        [Parameter(Mandatory = true, ValueFromPipeline = true,
            ParameterSetName = "Instance")]
        [ValidateNotNull]
        public User Instance { get; set; }

        protected override void ProcessRecord()
        {
            if (!this.CanFindAccount(Identity, AccountType.User))
            {
                return;
            }

            var name = ParameterSetName == "Id" ? Identity.Name : Instance.Name;

            var member = Membership.GetUser(name);
            if (member == null) return;

            member.IsApproved = true;

            if (ShouldProcess(member.UserName, "Disable user"))
            {
                Membership.UpdateUser(member);
            }
        }
    }
}