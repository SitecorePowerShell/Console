using System;
using System.Data;
using System.Management.Automation;
using System.Web.Security;
using Sitecore.Data;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Commandlets.Security.Accounts
{
    [Cmdlet(VerbsCommon.New, "User", DefaultParameterSetName = "Id", SupportsShouldProcess = true)]
    [OutputType(typeof (User))]
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

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public ID ProfileItemId { get; set; }

        protected override void ProcessRecord()
        {
            var name = Identity.Name;
            if (!ShouldProcess(Identity.Domain, "Create User '" + Identity.Account + "' in the domain")) return;

            if (User.Exists(name))
            {
                WriteError(typeof (DuplicateNameException), $"Cannot create a duplicate account with identity '{name}'.",
                    ErrorIds.AccountAlreadyExists, ErrorCategory.InvalidArgument, Identity);
                return;
            }

            var pass = Password;

            if (!Enabled)
            {
                if (string.IsNullOrEmpty(Password))
                {
                    pass = Membership.GeneratePassword(10, 3);
                }
            }

            var member = Membership.CreateUser(name, pass, Email);
            member.IsApproved = Enabled;
            Membership.UpdateUser(member);

            var user = User.FromName(name, true);

            var profile = user.Profile;
            if (!string.IsNullOrEmpty(FullName))
            {
                profile.FullName = FullName;
            }
            if (!string.IsNullOrEmpty(Comment))
            {
                profile.Comment = Comment;
            }
            if (!string.IsNullOrEmpty(Portrait))
            {
                profile.Portrait = Portrait;
            }
            if (!ID.IsNullOrEmpty(ProfileItemId))
            {
                profile.ProfileItemId = ProfileItemId.ToString();
            }
            profile.Save();

            WriteObject(user);
        }
    }
}