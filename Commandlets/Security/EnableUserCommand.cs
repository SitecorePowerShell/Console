using System.Management.Automation;
using System.Web.Security;
using Cognifide.PowerShell.Extensions;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Commandlets.Security
{
    [Cmdlet(VerbsLifecycle.Enable, "User", DefaultParameterSetName = "Id")]
    public class EnableUserCommand : BaseCommand
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

            member.IsApproved = true;

            Membership.UpdateUser(member);
        }
    }
}