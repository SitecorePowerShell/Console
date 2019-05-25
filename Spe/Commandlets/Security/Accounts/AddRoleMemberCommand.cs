using System.Data;
using System.Management.Automation;
using Sitecore.Security.Accounts;
using Spe.Core.Validation;

namespace Spe.Commandlets.Security.Accounts
{
    [Cmdlet(VerbsCommon.Add, "RoleMember", DefaultParameterSetName = "Id", SupportsShouldProcess = true)]
    public class AddRoleMemberCommand : BaseSecurityCommand
    {
        [Alias("Name")]
        [Parameter(ParameterSetName = "Id", ValueFromPipeline = true, Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        [AutocompleteSet(nameof(RoleNames))]
        public AccountIdentity Identity { get; set; }

        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        [AutocompleteSet(nameof(RoleAndUserNames))]
        public AccountIdentity[] Members { get; set; }

        protected override void ProcessRecord()
        {
            var name = Identity.Name;
            if (Role.Exists(name))
            {
                var targetRole = Role.FromName(name);

                foreach (var member in Members)
                {
                    if (User.Exists(member.Name))
                    {
                        var user = User.FromName(member.Name, false);
                        if (user.IsInRole(targetRole)) continue;

                        if (!ShouldProcess(targetRole.Name, $"Add user '{user.Name}' to role")) continue;

                        var profile = UserRoles.FromUser(user);
                        profile.Add(targetRole);
                    }
                    else if (Role.Exists(member.Name))
                    {
                        var role = Role.FromName(member.Name);
                        if (RolesInRolesManager.IsRoleInRole(role, targetRole, false)) continue;

                        if (ShouldProcess(targetRole.Name, $"Add role '{role.Name}' to role"))
                        {
                            RolesInRolesManager.AddRoleToRole(role, targetRole);
                        }
                    }
                    else
                    {
                        WriteError(typeof(ObjectNotFoundException), $"Cannot find an account with identity '{member}'.", 
                            ErrorIds.AccountNotFound, ErrorCategory.ObjectNotFound, member);
                    }
                }
            }
            else
            {
                WriteError(typeof(ObjectNotFoundException), $"Cannot find an account with identity '{name}'.", 
                    ErrorIds.AccountNotFound, ErrorCategory.ObjectNotFound, Identity);
            }
        }
    }
}