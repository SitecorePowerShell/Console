using System.Management.Automation;
using System.Web.Security;
using Sitecore.Security.Accounts;
using Spe.Core.Extensions;
using Spe.Core.Validation;

namespace Spe.Commandlets.Security.Accounts
{
    [Cmdlet(VerbsCommon.Unlock, "User", DefaultParameterSetName = "Id", SupportsShouldProcess = true)]
    public class UnlockUserCommand : BaseSecurityCommand
    {
        [Alias("Name")]
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0,
            ParameterSetName = "Id")]
        [ValidateNotNullOrEmpty]
        [AutocompleteSet(nameof(UserNames))]
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