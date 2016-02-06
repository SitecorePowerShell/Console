using System;
using System.Collections;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;
using System.Web.Security;
using System.Xml;
using Cognifide.PowerShell.Core.Validation;
using Sitecore.Analytics.Tracking;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Resources;
using Sitecore.Security.Accounts;
using Sitecore.Text;

namespace Cognifide.PowerShell.Commandlets.Security.Accounts
{
    [Cmdlet(VerbsCommon.Set, "User", DefaultParameterSetName = "Id", SupportsShouldProcess = true)]
    public class SetUserCommand : BaseCommand
    {
        private static readonly string[] Portraits;

        [Parameter]
        public bool IsAdministrator { get; set; }

        [Parameter]
        [AutocompleteSet("Portraits")]
        public string Portrait { get; set; }

        public SetUserCommand()
        {
            Enabled = true;

        }

        static SetUserCommand()
        {
            // Extracted example from Sitecore.Shell.Applications.Security.EditUser.EditUserPage.cs in Sitecore.Client.dll
            Portraits = (Factory.GetConfigNodes("portraits/collection")
                .Cast<XmlNode>()
                .SelectMany(xmlNode => new ListString(xmlNode.InnerText),
                    (xmlNode, src) => ImageBuilder.ResizeImageSrc(src, 16, 16).Trim())).ToArray();
        }

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
        [ValidatePattern("^\\w+([-+.']\\w+)*@\\w+([-.]\\w+)*\\.\\w+([-.]\\w+)*$",
            Options = RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        public string Email { get; set; }

        [Parameter]
        public string FullName { get; set; }

        [Parameter]
        public string Comment { get; set; }

        [Parameter]
        public ID ProfileItemId { get; set; }

        [Parameter]
        [ValidateSet("Default", "ContentEditor", "PageEditor", "Preview", "Desktop")]
        public string StartUrl { get; set; }

        [Parameter]
        public SwitchParameter Enabled { get; set; }

        [Parameter]
        public Hashtable CustomProperties { get; set; }

        protected override void ProcessRecord()
        {

            var user = Instance;
            if (ParameterSetName == "Id")
            {
                user = User.FromName(Identity.Name, true);
            }
            var name = user.Name;

            if (!ShouldProcess(name, "Set User information"))
            {
                return;
            }

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

            if (User.Current.IsAdministrator && IsParameterSpecified("IsAdministrator"))
            {
                profile.IsAdministrator = IsAdministrator;
            }

            if (!string.IsNullOrEmpty(StartUrl))
            {
                switch (StartUrl)
                {
                    case "ContentEditor":
                        profile.StartUrl = "/sitecore/shell/applications/clientusesoswindows.aspx";
                        break;
                    case "PageEditor":
                        profile.StartUrl = "/sitecore/shell/applications/webedit.aspx";
                        break;
                    case "Preview":
                        profile.StartUrl = "/sitecore/shell/applications/preview.aspx";
                        break;
                    case "Desktop":
                        profile.StartUrl = "/sitecore/shell/default.aspx";
                        break;
                    default:
                        profile.StartUrl = string.Empty;
                        break;
                }
            }

            if (CustomProperties != null)
            {
                var propertyNames = profile.GetCustomPropertyNames();

                foreach (var key in CustomProperties.Keys)
                {
                    var propertyName =
                        propertyNames.FirstOrDefault(p => p.Equals(key.ToString(), StringComparison.OrdinalIgnoreCase));
                    if (string.IsNullOrEmpty(propertyName))
                    {
                        propertyName = key.ToString();
                    }

                    var property = CustomProperties[key] ?? string.Empty;
                    profile.SetCustomProperty(propertyName, property.ToString());
                }
            }

            profile.Save();

            if (!Enabled.IsPresent) return;

            var member = Membership.GetUser(name);
            if (member == null) return;

            member.IsApproved = Enabled;

            Membership.UpdateUser(member);
        }
    }
}