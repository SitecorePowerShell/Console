using System.Management.Automation;
using System.Web.Security;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Commandlets.Security.Accounts
{
    [Cmdlet(VerbsCommon.Unlock, "User", DefaultParameterSetName = "Id", SupportsShouldProcess = true)]
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
            if (!this.CanFindAccount(Identity, AccountType.User))
            {
                return;
            }

            var name = ParameterSetName == "Id" ? Identity.Name : Instance.Name;

            var member = Membership.GetUser(name);
            if (member == null) return;

            if (!ShouldProcess(name, "Unlock User")) return;

            member.UnlockUser();
            Membership.UpdateUser(member);
        }
    }
}