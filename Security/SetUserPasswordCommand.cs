using System;
using System.Data;
using System.Management.Automation;
using System.Web.Security;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Security
{
    [Cmdlet(VerbsCommon.Set, "UserPassword", DefaultParameterSetName = "Set password")]
    public class SetUserPasswordCommand : BaseCommand
    {
        [Alias("Name")]
        [Parameter(ParameterSetName = "Set password", ValueFromPipeline = true, Mandatory = true, Position = 0)]
        [Parameter(ParameterSetName = "Reset password", ValueFromPipeline = true, Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public AccountIdentity Identity { get; set; }

        [ValidateNotNullOrEmpty]
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string NewPassword { get; set; }

        [ValidateNotNullOrEmpty]
        [Parameter(ParameterSetName = "Set password", Mandatory = true)]
        public string OldPassword { get; set; }

        [Parameter(ParameterSetName = "Reset password", Mandatory = true)]
        public SwitchParameter Reset { get; set; }

        protected override void ProcessRecord()
        {
            var name = Identity.Name;

            if (!User.Exists(name))
            {
                var error = String.Format("Cannot find an account with identity '{0}'.", name);
                WriteError(new ErrorRecord(new ObjectNotFoundException(error), error, ErrorCategory.ObjectNotFound, Identity));
            }

            var oldpassword = OldPassword;

            var member = Membership.GetUser(name);
            if (member == null) return;

            if (Reset.IsPresent && User.Current.IsAdministrator)
            {
                oldpassword = member.ResetPassword();
            }

            member.ChangePassword(oldpassword, NewPassword);
        }
    }
}