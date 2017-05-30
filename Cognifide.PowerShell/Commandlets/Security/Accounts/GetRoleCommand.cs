using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Validation;
using Sitecore;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Commandlets.Security.Accounts
{
    [Cmdlet(VerbsCommon.Get, "Role", DefaultParameterSetName = "Id")]
    [OutputType(typeof (Role))]
    public class GetRoleCommand : BaseSecurityCommand
    {
        [Alias("Name")]
        [Parameter(ParameterSetName = "Id", ValueFromPipeline = true, Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        [AutocompleteSet(nameof(RoleNames))]
        public AccountIdentity Identity { get; set; }

        [Parameter(ParameterSetName = "Filter", Mandatory = true)]
        [ValidateNotNullOrEmpty]
        [AutocompleteSet(nameof(RoleNames))]
        public string Filter { get; set; }

        protected override void ProcessRecord()
        {
            if (ParameterSetName == "Filter")
            {
                var filter = Filter;

                var managedRoles = Context.User.Delegation.GetManagedRoles(true);
                WildcardWrite(filter, managedRoles, role => role.Name);
            }
            else
            {
                if (!this.CanFindAccount(Identity, AccountType.Role))
                {
                    return;
                }

                WriteObject(Role.FromName(Identity.Name));
            }
        }
    }
}