using System;
using System.Data;
using System.Management.Automation;
using System.Web.Security;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Commandlets.Security
{
    [Cmdlet(VerbsCommon.New, "User", DefaultParameterSetName = "Id", SupportsShouldProcess = true)]
    [OutputType(new[] {typeof (User)})]
    public class NewUserCommand : BaseCommand
    {
        [Alias("Name")]
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0,
            ParameterSetName = "Id")]
        [ValidateNotNullOrEmpty]
        public AccountIdentity Identity { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string Password { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string Email { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string FullName { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string Comment { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string Portrait { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public SwitchParameter Enabled { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            var name = Identity.Name;
            if (ShouldProcess(Identity.Domain, "Create User '" +Identity.Account + "' in the domain"))
            {
                if (User.Exists(name))
                {
                    var error = String.Format("Cannot create a duplicate account with identity '{0}'.", name);
                    WriteError(new ErrorRecord(new DuplicateNameException(error), error, ErrorCategory.InvalidArgument,
                        Identity));
                    return;
                }

                var pass = Password;

                if (!Enabled)
                {
                    if (String.IsNullOrEmpty(Password))
                    {
                        pass = Membership.GeneratePassword(10, 3);
                    }
                }

                var member = Membership.CreateUser(name, pass, Email);
                member.IsApproved = Enabled;
                Membership.UpdateUser(member);

                var user = User.FromName(name, true);

                var profile = user.Profile;
                if (!String.IsNullOrEmpty(FullName))
                {
                    profile.FullName = FullName;
                }
                if (!String.IsNullOrEmpty(Comment))
                {
                    profile.Comment = Comment;
                }
                if (!String.IsNullOrEmpty(Portrait))
                {
                    profile.Portrait = Portrait;
                }
                profile.Save();

                if (PassThru)
                {
                    WriteObject(user);
                }
            }
        }
    }
}