using System.Data;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Commandlets.Security.Accounts
{
    [Cmdlet(VerbsCommon.Remove, "User", DefaultParameterSetName = "Id", SupportsShouldProcess = true)]
    public class RemoveUserCommand : BaseCommand
    {
        [Alias("Name")]
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0,
            ParameterSetName = "Id")]
        [ValidateNotNullOrEmpty]
        public AccountIdentity Identity { get; set; }

        [Parameter(Mandatory = true, ValueFromPipeline = true,
            ParameterSetName = "Instance")]
        [ValidateNotNull]
        public User Instance { get; set; }

        protected override void ProcessRecord()
        {
            if (Identity != null && !this.CanFindAccount(Identity, AccountType.User))
            {
                WriteError(typeof (ObjectNotFoundException), $"User '{Identity.Name}' not found.",
                    ErrorIds.AccountNotFound, ErrorCategory.ResourceUnavailable, Identity);
                return;
            }

            var name = Identity?.Name ?? Instance?.Name ?? string.Empty;

            if (!ShouldProcess(name, "Remove user")) return;

            var user = User.FromName(name, true);
            user.Delete();
        }
    }
}