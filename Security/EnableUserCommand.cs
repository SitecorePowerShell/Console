using System;
using System.Data;
using System.Management.Automation;
using System.Web.Security;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Security
{
    [Cmdlet(VerbsLifecycle.Enable, "User", DefaultParameterSetName = "Id")]
    public class EnableUserCommand : BaseCommand
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
            var name = ParameterSetName == "Id" ? Identity.Name : Instance.Name;

            if (User.Exists(name))
            {
                var member = Membership.GetUser(name);
                if (member == null) return;

                member.IsApproved = true;

                Membership.UpdateUser(member);
            }
            else
            {
                var error = String.Format("Cannot find an account with identity '{0}'.", name);
                WriteError(new ErrorRecord(new ObjectNotFoundException(error), error, ErrorCategory.ObjectNotFound, Identity));
            }
        }
    }
}