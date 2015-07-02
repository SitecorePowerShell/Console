using System;
using System.Data;
using System.Management.Automation;
using System.Web.Security;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Commandlets.Security.Accounts
{
    [Cmdlet(VerbsCommon.New, "Role", DefaultParameterSetName = "Id")]
    [OutputType(typeof (Role))]
    public class NewRoleCommand : BaseCommand
    {
        [Alias("Name")]
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0, ParameterSetName = "Id")]
        [ValidateNotNullOrEmpty]
        public AccountIdentity Identity { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            var name = Identity.Name;
            if (ShouldProcess(Identity.Domain, "Create Role '" + Identity.Account + "' in the domain"))
            {
                if (Role.Exists(name))
                {
                    var error = String.Format("Cannot create a duplicate account with identity '{0}'.", name);
                    WriteError(new ErrorRecord(new DuplicateNameException(error), error, ErrorCategory.InvalidArgument,
                        Identity));
                    return;
                }

                Roles.CreateRole(name);
                var role = Role.FromName(name);

                if (PassThru)
                {
                    WriteObject(role);
                }
            }
        }
    }
}