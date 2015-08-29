using System;
using System.Data;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Commandlets.Security.Accounts
{
    [Cmdlet(VerbsCommon.Remove, "RoleMember", DefaultParameterSetName = "Id", SupportsShouldProcess = true)]
    public class RemoveRoleMemberCommand : BaseCommand
    {
        [Alias("Name")]
        [Parameter(ParameterSetName = "Id", ValueFromPipeline = true, Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public AccountIdentity Identity { get; set; }

        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public AccountIdentity[] Members { get; set; }

        protected override void ProcessRecord()
        {
            if (!this.CanFindAccount(Identity, AccountType.Role))
            {
                return;
            }

            var name = Identity.Name;

            var targetRole = Role.FromName(name);

            foreach (var member in Members)
            {
                if (User.Exists(member.Name))
                {
                    var user = User.FromName(member.Name, false);
                    if (!user.IsInRole(targetRole)) continue;

                    var profile = UserRoles.FromUser(user);
                    if (ShouldProcess(targetRole.Name, $"Remove user '{user.Name}' from role"))
                    {
                        profile.Remove(targetRole);
                    }
                }
                else if (Role.Exists(member.Name))
                {
                    var role = Role.FromName(member.Name);
                    if (!RolesInRolesManager.IsRoleInRole(role, targetRole, false))
                    {
                        if (ShouldProcess(targetRole.Name, $"Remove role '{role.Name}' from role"))
                        {
                            RolesInRolesManager.RemoveRoleFromRole(role, targetRole);
                        }
                    }
                }
                else
                {
                    WriteError(typeof(ObjectNotFoundException), $"Cannot find an account with identity '{member}'.",
                        ErrorIds.AccountNotFound, ErrorCategory.ObjectNotFound, member);
                }
            }
        }
    }
}