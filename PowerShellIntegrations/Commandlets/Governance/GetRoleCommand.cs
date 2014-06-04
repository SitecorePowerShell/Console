using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Management.Automation;
using Sitecore;
using Sitecore.Security.Accounts;
using Sitecore.Shell.Applications.Security.RoleManager;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Governance
{
    [Cmdlet(VerbsCommon.Get, "Role", DefaultParameterSetName = "User from name")]
    [OutputType(new[] {typeof (User)})]
    public class GetRoleCommand : BaseCommand
    {
        [Parameter(ValueFromPipeline = true, Mandatory = true, Position = 0)]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            string name = Name;

            if (string.IsNullOrEmpty(name))
            {
                WriteObject(Context.User.Delegation.GetManagedRoles(true), true);
                return;
            }

            if (name.Contains('?') || name.Contains('*'))
            {
                var managedRoles = Context.User.Delegation.GetManagedRoles(true);
                WildcardWrite(name, managedRoles, role => role.Name);
                return;
            }

            if (!name.Contains(@"\"))
            {
                name = @"sitecore\" + name;
            }

            if (Role.Exists(name))
                WriteObject(Role.FromName(name));
            else
            {
                WriteError(new ErrorRecord(new ObjectNotFoundException("Role '" + name + "' could not be found"),
                    "sitecore_role_not_found", ErrorCategory.ObjectNotFound, null));
            }
        }
    }
}