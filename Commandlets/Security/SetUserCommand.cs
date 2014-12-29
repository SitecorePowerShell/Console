using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;
using System.Web.Security;
using System.Xml;
using Cognifide.PowerShell.Extensions;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Resources;
using Sitecore.Security.Accounts;
using Sitecore.Text;

namespace Cognifide.PowerShell.Commandlets.Security
{
    [Cmdlet(VerbsCommon.Set, "User", DefaultParameterSetName = "Id")]
    public class SetUserCommand : BaseCommand, IDynamicParameters
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

        private static string[] portraits;

        static SetUserCommand()
        {
            var validValues = new List<string>();
            // Extracted example from Sitecore.Shell.Applications.Security.EditUser.EditUserPage.cs in Sitecore.Client.dll
            foreach (XmlNode xmlNode in Factory.GetConfigNodes("portraits/collection"))
            {
                foreach (var src in new ListString(xmlNode.InnerText))
                {
                    validValues.Add(ImageBuilder.ResizeImageSrc(src, 16, 16).Trim());
                }
            }
            portraits = validValues.ToArray();
        }

        public SetUserCommand()
        {
            Enabled = true;

            AddDynamicParameter<string>("Portrait", new Attribute[]
            {
                new ParameterAttribute
                {
                    ParameterSetName = ParameterAttribute.AllParameterSets
                },
                new ValidateSetAttribute(portraits)
            });

            if (User.Current.IsAdministrator)
            {
                AddDynamicParameter<SwitchParameter>("IsAdministrator", new Attribute[]
            {
                new ParameterAttribute
                {
                    ParameterSetName = ParameterAttribute.AllParameterSets
                }
            });
            }
        }

        [Parameter]
        public SwitchParameter Enabled { get; set; }

        [Parameter]
        public Hashtable CustomProperties { get; set; }

        protected override void ProcessRecord()
        {
            if (!this.CanFindAccount(Identity, AccountType.User)) { return; }

            var name = ParameterSetName == "Id" ? Identity.Name : Instance.Name;

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

            var portrait = String.Empty;
            if (TryGetParameter("Portrait", out portrait))
            {
                if (!String.IsNullOrEmpty(portrait))
                {
                    profile.Portrait = portrait;
                }
            }

            if (User.Current.IsAdministrator)
            {
                var isPresent = false;
                var isAdmin = false;
                if (TryGetSwitchParameter("IsAdministrator", out isPresent, out isAdmin))
                {
                    profile.IsAdministrator = isAdmin;
                }
            }

            if (!String.IsNullOrEmpty(StartUrl))
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
                        profile.StartUrl = String.Empty;
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
                    if (String.IsNullOrEmpty(propertyName))
                    {
                        propertyName = key.ToString();
                    }

                    var property = CustomProperties[key] ?? String.Empty;
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