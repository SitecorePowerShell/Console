using System;
using System.Data;
using System.Management.Automation;
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
            var name = Identity.Name;

            if (Role.Exists(name))
            {
                var role = Role.FromName(name);
                switch (ParameterSetName)
                {
                    case "Id" :
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
            else
            {
                var error = String.Format("Cannot find an account with identity '{0}'.", name);
                WriteError(new ErrorRecord(new ObjectNotFoundException(error), error, ErrorCategory.ObjectNotFound,
                    Identity));
            }
        }
    }
}