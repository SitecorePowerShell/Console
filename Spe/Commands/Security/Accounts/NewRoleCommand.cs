using System.Data;
using System.Management.Automation;
using System.Web.Security;
using Sitecore.Security.Accounts;
using Spe.Core.Validation;

namespace Spe.Commands.Security.Accounts
{
    [Cmdlet(VerbsCommon.New, "Role", DefaultParameterSetName = "Id")]
    [OutputType(typeof (Role))]
    public class NewRoleCommand : BaseSecurityCommand
    {
        [Alias("Name")]
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0, ParameterSetName = "Id")]
        [ValidateNotNullOrEmpty]
        [AutocompleteSet(nameof(RoleNames))]
        public AccountIdentity Identity { get; set; }

        protected override void ProcessRecord()
        {
            var name = Identity.Name;
            if (!ShouldProcess(Identity.Domain, "Create Role '" + Identity.Account + "' in the domain")) return;

            if (Role.Exists(name))
            {
                WriteError(typeof(DuplicateNameException), $"Cannot create a duplicate account with identity '{name}'.", 
                    ErrorIds.AccountAlreadyExists, ErrorCategory.InvalidArgument, Identity);
                return;
            }

            Roles.CreateRole(name);
            var role = Role.FromName(name);

            WriteObject(role);
        }
    }
}