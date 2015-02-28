using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Security;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore;
using Sitecore.Data.Serialization;
using Sitecore.Security.Accounts;
using Sitecore.Security.Serialization;

namespace Cognifide.PowerShell.Commandlets.Serialization
{
    [Cmdlet(VerbsData.Export, "User", SupportsShouldProcess = true)]
    public class ExportUserCommand : BaseCommand
    {
        [Alias("Name")]
        [Parameter(ParameterSetName = "Id", ValueFromPipeline = true, Mandatory = true, Position = 0)]
        [Parameter(ParameterSetName = "Path Id", ValueFromPipeline = true, Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public AccountIdentity Identity { get; set; }

        [Parameter(ParameterSetName = "Filter", Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Filter { get; set; }

        [Parameter(ParameterSetName = "User", Mandatory = true, ValueFromPipeline = true, Position = 0)]
        [Parameter(ParameterSetName = "Path User", Mandatory = true, ValueFromPipeline = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public User User { get; set; }

        [Parameter(ParameterSetName = "Current", Mandatory = true)]
        [Parameter(ParameterSetName = "Path Current", Mandatory = true)]
        public SwitchParameter Current { get; set; }

        [Parameter(ParameterSetName = "Path Id", Mandatory = true)]
        [Parameter(ParameterSetName = "Path User", Mandatory = true)]
        [Parameter(ParameterSetName = "Path Current", Mandatory = true)]
        [Alias("FullName", "FileName")]
        public string Path { get; set; }

        [Parameter(ParameterSetName = "Id")]
        [Parameter(ParameterSetName = "Filter")]
        [Parameter(ParameterSetName = "User")]
        [Parameter(ParameterSetName = "Current")]
        [Alias("Target")]
        public string Root { get; set; }

        protected override void ProcessRecord()
        {
            switch (ParameterSetName)
            {
                case "Current":
                case "Path Current":
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
                    if (!this.CanFindAccount(Identity, AccountType.User))
                    {
                        return;
                    }

                    SerializeUser(User.FromName(Identity.Name, true));
                    break;
            }
        }

        private void SerializeUser(User user)
        {
            if (string.IsNullOrEmpty(Root) && string.IsNullOrEmpty(Path))
            {
                if (ShouldProcess(user.Name, string.Format("Serializing user")))
                {
                    var logMessage = string.Format("Serializing user '{0}'", user.Name);
                    WriteVerbose(logMessage);
                    WriteDebug(logMessage);
                    Manager.DumpUser(user.Name);
                    WriteObject(PathUtils.GetFilePath(new UserReference(user.Name)));
                }
            }
            else
            {
                var userReference = new UserReference(user.Name);
                if (string.IsNullOrEmpty(Path))
                {
                    if (string.IsNullOrEmpty(Root))
                    {
                        Path = PathUtils.GetFilePath(userReference);
                    }
                    else
                    {
                        var target = Root.EndsWith("\\") ? Root : Root + "\\";
                        Path = (target + userReference).Replace('/', System.IO.Path.DirectorySeparatorChar);
                    }
                    if (!System.IO.Path.HasExtension(Path))
                    {
                        Path += PathUtils.UserExtension;
                    }
                }

                if (ShouldProcess(user.Name, string.Format("Serializing role to '{0}'", Path)))
                {
                    var logMessage = string.Format("Serializing user '{0}' to '{1}'", user.Name, Path);
                    WriteVerbose(logMessage);
                    WriteDebug(logMessage);
                    Manager.DumpUser(Path, userReference.User);
                    WriteObject(Path);
                }
            }
        }
    }
}