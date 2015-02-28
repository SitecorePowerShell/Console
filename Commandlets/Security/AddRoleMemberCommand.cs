using System;
using System.Data;
using System.Management.Automation;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Commandlets.Security
{
    [Cmdlet(VerbsCommon.Add, "RoleMember", DefaultParameterSetName = "Id", SupportsShouldProcess = true)]
    public class AddRoleMemberCommand : BaseCommand
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

                        if (ShouldProcess(targetRole.Name, string.Format("Add user '{0}' to role", user.Name)))
                        {
                            var profile = UserRoles.FromUser(user);
                            profile.Add(targetRole);
                        }
                    }
                    else if (Role.Exists(member.Name))
                    {
                        var role = Role.FromName(member.Name);
                        if (!RolesInRolesManager.IsRoleInRole(role, targetRole, false))
                        {
                            if (ShouldProcess(targetRole.Name, string.Format("Add role '{0}' to role", role.Name)))
                            {
                                RolesInRolesManager.AddRoleToRole(role, targetRole);
                            }
                        }
                    }
                    else
                    {
                        var error = String.Format("Cannot find an account with identity '{0}'.", member);
                        WriteError(new ErrorRecord(new ObjectNotFoundException(error), error,
                            ErrorCategory.ObjectNotFound, member));
                    }
                }
            }
            else
            {
                var error = String.Format("Cannot find an account with identity '{0}'.", name);
                WriteError(new ErrorRecord(new ObjectNotFoundException(error), error, ErrorCategory.ObjectNotFound,
                    Identity));
            }
        }
    }
}