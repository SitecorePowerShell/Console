using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Security;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Serialization;
using Sitecore;
using Sitecore.Data.Serialization;
using Sitecore.Data.Serialization.Presets;
using Sitecore.Security.Accounts;
using Sitecore.Security.Serialization;

namespace Cognifide.PowerShell.Commandlets.Serialization
{
    [Cmdlet("Export", "User")]
    public class ExportUserCommand : BaseCommand
    {

        [Alias("Name")]
        [Parameter(ParameterSetName = "Id", ValueFromPipeline = true, Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public AccountIdentity Identity { get; set; }

        [Parameter(ParameterSetName = "Filter", Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Filter { get; set; }

        [Parameter(ParameterSetName = "User", Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public User User { get; set; }

        [Parameter(ParameterSetName = "Current", Mandatory = true)]
        public SwitchParameter Current { get; set; }

        [Parameter]
        [Alias("FullName", "FileName")]
        public string Path { get; set; }

        [Parameter]
        [Alias("Target")]
        public string Root { get; set; }

        protected override void ProcessRecord()
        {
            switch (ParameterSetName)
            {
                case "Current":
                    SerializeUser(Context.User);
                    break;
                case "User":
                    SerializeUser(User);
                    break;
                case "Filter":
                    var filter = Filter;
                    if (!filter.Contains("?") && !filter.Contains("*")) return;

                    var users = WildcardFilter(filter, UserManager.GetUsers(), user => user.Name);
                    users.ToList().ForEach(SerializeUser)
                    ;
                    break;
                default:
                    if (!this.CanFindAccount(Identity, AccountType.User)) { return; }

                    SerializeUser(User.FromName(Identity.Name, true));
                    break;
            }
        }

        private void SerializeUser(User user)
        {
            var target = string.IsNullOrEmpty(Root) || Root.EndsWith("\\") ? Root : Root + "\\";

            var logMessage = string.Format("Serializing user '{0}' to target '{1}'", user.Name, target);
            WriteVerbose(logMessage);
            WriteDebug(logMessage);

            if (string.IsNullOrEmpty(target))
            {
                Manager.DumpUser(user.Name);
            }
            else
            {
                UserReference userReference = new UserReference(user.Name);
                Manager.DumpUser((target + userReference + PathUtils.UserExtension).Replace('/', System.IO.Path.DirectorySeparatorChar), userReference.User);
            }
        }
    }
}