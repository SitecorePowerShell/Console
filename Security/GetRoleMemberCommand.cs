using System.Management.Automation;
using Cognifide.PowerShell.Extensions;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Security
{
    [Cmdlet(VerbsCommon.Get, "RoleMember", DefaultParameterSetName = "Id")]
    [OutputType(new[] {typeof (Role), typeof (User)})]
    public class GetRoleMemberCommand : BaseCommand
    {
        [Alias("Name")]
        [Parameter(ParameterSetName = "Id", ValueFromPipeline = true, Mandatory = true, Position = 0)]
        [Parameter(ParameterSetName = "UsersOnly", ValueFromPipeline = true, Mandatory = true, Position = 0)]
        [Parameter(ParameterSetName = "ComputersOnly", ValueFromPipeline = true, Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public AccountIdentity Identity { get; set; }

        [Parameter]
        public SwitchParameter Recursive { get; set; }

        [Parameter(ParameterSetName = "UsersOnly")]
        public SwitchParameter UsersOnly { get; set; }

        [Parameter(ParameterSetName = "ComputersOnly")]
        public SwitchParameter RolesOnly { get; set; }

        protected override void ProcessRecord()
        {
            if (!this.CanFindAccount(Identity, AccountType.User)) { return; }

            var role = Role.FromName(Identity.Name);
            switch (ParameterSetName)
            {
                case "Id":
                    WriteObject(RolesInRolesManager.GetRoleMembers(role, Recursive));
                    break;
                case "UsersOnly":
                    WriteObject(RolesInRolesManager.GetUsersInRole(role, Recursive));
                    break;
                case "ComputersOnly":
                    WriteObject(RolesInRolesManager.GetRolesInRole(role, Recursive));
                    break;
            }
        }
    }
}