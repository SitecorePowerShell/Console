using System.Linq;
using System.Management.Automation;
using System.Web.Security;
using Sitecore.Exceptions;
using Sitecore.Security.Accounts;
using Spe.Core.Extensions;
using Spe.Core.Validation;

namespace Spe.Commandlets.Security.Accounts
{
    [Cmdlet(VerbsCommon.Remove, "Role", DefaultParameterSetName = "Id", SupportsShouldProcess = true)]
    public class RemoveRoleCommand : BaseSecurityCommand
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
        public Role Instance { get; set; }

        protected override void ProcessRecord()
        {
            if (!this.CanFindAccount(Identity, AccountType.Role))
            {
                return;
            }

            var name = ParameterSetName == "Id" ? Identity.Name : Instance.Name;

            if (!ShouldProcess(name, "Remove role")) return;

            var role = Role.FromName(name);
            if (!role.IsEveryone)
            {
                var usersInRoles = Roles.GetUsersInRole(name);
                if (usersInRoles.Any())
                {
                    Roles.RemoveUsersFromRole(usersInRoles, name);
                }

                if (RolesInRolesManager.RolesInRolesSupported)
                {
                    var rolesInRole = RolesInRolesManager.GetRolesForRole(role, false);
                    if (rolesInRole.Any())
                    {
                        RolesInRolesManager.RemoveRolesFromRole(rolesInRole, role);
                    }
                }

                Roles.DeleteRole(name, true);
            }
            else
            {
                WriteError(typeof(SecurityException), $"Cannot remove role '{name}'.", 
                    ErrorIds.InsufficientSecurityRights, ErrorCategory.PermissionDenied, name);
            }
        }
    }
}