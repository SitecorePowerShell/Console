using System.Management.Automation;
using System.Web.Security;
using Cognifide.PowerShell.Extensions;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Security
{
    [Cmdlet(VerbsCommon.Unlock, "User", DefaultParameterSetName = "Id")]
    public class UnlockUserCommand : BaseCommand
    {
        [Alias("Name")]
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0,
            ParameterSetName = "Id")]
        [ValidateNotNullOrEmpty]
        public AccountIdentity Identity { get; set; }

        [Parameter(Mandatory = true, ValueFromPipeline = true,
            ParameterSetName = "Instance")]
        [ValidateNotNull]
        public User Instance { get; set; }

        protected override void ProcessRecord()
        {
            if (!this.CanFindAccount(Identity, AccountType.User)) { return; }

            var name = ParameterSetName == "Id" ? Identity.Name : Instance.Name;

            var member = Membership.GetUser(name);
            if (member == null) return;

            member.UnlockUser();

            Membership.UpdateUser(member);
        }
    }
}