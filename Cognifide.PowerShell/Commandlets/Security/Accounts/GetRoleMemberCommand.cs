using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Validation;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Commandlets.Security.Accounts
{
    [Cmdlet(VerbsCommon.Get, "RoleMember", DefaultParameterSetName = "Id")]
    [OutputType(typeof (Role), typeof (User))]
    public class GetRoleMemberCommand : BaseSecurityCommand
    {
        [Alias("Name")]
        [Parameter(ParameterSetName = "Id", ValueFromPipeline = true, Mandatory = true, Position = 0)]
        [Parameter(ParameterSetName = "UsersOnly", ValueFromPipeline = true, Mandatory = true, Position = 0)]
        [Parameter(ParameterSetName = "RolesOnly", ValueFromPipeline = true, Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        [AutocompleteSet(nameof(RoleNames))]
        public AccountIdentity Identity { get; set; }

        [Parameter]
        [Alias("Recursive")]
        public SwitchParameter Recurse { get; set; }

        [Parameter(ParameterSetName = "UsersOnly")]
        public SwitchParameter UsersOnly { get; set; }

        [Parameter(ParameterSetName = "RolesOnly")]
        public SwitchParameter RolesOnly { get; set; }

        protected override void ProcessRecord()
        {
            if (!this.CanFindAccount(Identity, AccountType.Role))
            {
                return;
            }

            var role = Role.FromName(Identity.Name);
            switch (ParameterSetName)
            {
                case "Id":
                    WriteObject(RolesInRolesManager.GetRoleMembers(role, Recurse), true);
                    break;
                case "UsersOnly":
                    WriteObject(RolesInRolesManager.GetUsersInRole(role, Recurse), true);
                    break;
                case "RolesOnly":
                    WriteObject(RolesInRolesManager.GetRolesInRole(role, Recurse), true);
                    break;
            }
        }
    }
}