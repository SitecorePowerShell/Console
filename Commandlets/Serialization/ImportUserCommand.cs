using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Security;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Serialization;
using Sitecore;
using Sitecore.Data.Serialization;
using Sitecore.Data.Serialization.Presets;
using Sitecore.Rules.Conditions.SecurityConditions;
using Sitecore.Security.Accounts;
using Sitecore.Security.Serialization;

namespace Cognifide.PowerShell.Commandlets.Serialization
{
    [Cmdlet("Import", "User")]
    public class ImportUserCommand : BaseCommand
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

        [Parameter(ParameterSetName = "Path")]
        [Alias("FullName", "FileName")]
        public string Path { get; set; }

        [Parameter(ParameterSetName = "Filter")]
        [Parameter(ParameterSetName = "User")]
        [Parameter(ParameterSetName = "Name")]
        public string Root { get; set; }

        protected override void ProcessRecord()
        {
            if (string.IsNullOrEmpty(Root))
            {
                Root = PathUtils.Root + "security\\";
            }
            switch (ParameterSetName)
            {
                case "User":
                    DeserializeUser(User.Name);
                    break;
                case "Filter":
                    var filter = Filter;
                    if (!filter.Contains("?") && !filter.Contains("*")) return;
                    DeserializeUser(filter);
                    break;
                case "Path":
                    DeserializeUser(Path);
                    break;
                default:
                    DeserializeUser(Identity.Name);
                    break;
            }
        }

        private void DeserializeUser(string userName)
        {
            var fileName = userName;

            // if path is not absolute - add the Root folder
            if (!System.IO.Path.IsPathRooted(fileName))
            {
                var identity = new AccountIdentity(userName);
                var target = string.IsNullOrEmpty(Root) || Root.EndsWith("\\") ? Root : Root + "\\";
                fileName = target + identity.Domain + @"\Users\" + identity.Account + PathUtils.UserExtension;
            }
            
            // make sure the path has the proper extension
            if (!fileName.EndsWith(PathUtils.UserExtension, StringComparison.OrdinalIgnoreCase))
            {
                fileName += PathUtils.UserExtension;
            }

            if (fileName.Contains("?") || fileName.Contains("*"))
            {
                var users = System.IO.Path.GetDirectoryName(fileName);
                var domainName = System.IO.Path.GetDirectoryName(users);
                var root = System.IO.Path.GetDirectoryName(domainName);
                foreach (var domain in Directory.EnumerateDirectories(root))
                {
                    var files = WildcardFilter(fileName, Directory.EnumerateFiles(domain + @"\Users"), f => f).ToList();
                    foreach (var file in files)
                    {
                        DeserializeUserFile(userName, file);
                    }
                }
            }
            DeserializeUserFile(userName, fileName);
        }

        private void DeserializeUserFile(string userName, string file)
        {
            var logMessage = string.Format("Deserializing user '{0}' from '{1}'", userName, file);
            WriteVerbose(logMessage);
            WriteDebug(logMessage);
        }
    }
}