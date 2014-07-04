using System;
using System.Collections;
using System.Data;
using System.Linq;
using System.Management.Automation;
using System.Web.Security;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Security
{
    [Cmdlet(VerbsCommon.Set, "User", DefaultParameterSetName = "Id")]
    public class SetUserCommand : BaseCommand
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

        [Parameter]
        public string Email { get; set; }

        [Parameter]
        public string FullName { get; set; }

        [Parameter]
        public string Comment { get; set; }

        [Parameter]
        public string Portrait { get; set; }

        public SetUserCommand()
        {
            Enabled = true;
        }

        [Parameter]
        public SwitchParameter Enabled { get; set; }

        [Parameter]
        public Hashtable CustomProperties { get; set; }

        protected override void ProcessRecord()
        {
            var name = ParameterSetName == "Id" ? Identity.Name : Instance.Name;

            if (!User.Exists(name))
            {
                var error = String.Format("Cannot find an account with identity '{0}'.", name);
                WriteError(new ErrorRecord(new ObjectNotFoundException(error), error, ErrorCategory.ObjectNotFound, Identity));
            }

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

            if (CustomProperties != null)
            {
                var propertyNames = profile.GetCustomPropertyNames();

                foreach (var key in CustomProperties.Keys)
                {
                    var propertyName = propertyNames.FirstOrDefault(p => p.Equals(key.ToString(), StringComparison.OrdinalIgnoreCase));
                    if (String.IsNullOrEmpty(propertyName))
                    {
                        propertyName = key.ToString();
                    }

                    var property = CustomProperties[key] ?? String.Empty;
                    profile.SetCustomProperty(propertyName, property.ToString());
                }
            }

            profile.Save();

            if (Enabled.IsPresent)
            {
                var member = Membership.GetUser(name);
                if (member == null) return;

                member.IsApproved = Enabled;

                Membership.UpdateUser(member);
            }
        }
    }
}