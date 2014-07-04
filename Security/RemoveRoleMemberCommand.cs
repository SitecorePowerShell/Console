using System;
using System.Data;
using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Security
{
    [Cmdlet(VerbsCommon.Remove, "RoleMember", DefaultParameterSetName = "Id")]
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
            var name = Identity.Name;

            if (Role.Exists(name))
            {
                var role = Role.FromName(name);

                foreach (var member in Members)
                {
                    // TODO: Remove roles nested roles too.
                    if (!User.Exists(member.Name)) continue;

                    var user = User.FromName(member.Name, false);
                    var profile = UserRoles.FromUser(user);
                    profile.Remove(role);
                }
            }
            else
            {
                var error = String.Format("Cannot find an account with identity '{0}'.", name);
                WriteError(new ErrorRecord(new ObjectNotFoundException(error), error, ErrorCategory.ObjectNotFound, Identity));
            }
        }
    }
}