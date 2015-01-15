using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Security;
using Sitecore.Data.Serialization;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Commandlets.Serialization
{
    [Cmdlet(VerbsData.Import, "Role")]
    public class ImportRoleCommand : BaseCommand
    {

        [Alias("Name")]
        [Parameter(ParameterSetName = "Id", ValueFromPipeline = true, Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public AccountIdentity Identity { get; set; }

        [Parameter(ParameterSetName = "Filter", Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Filter { get; set; }

        [Parameter(ParameterSetName = "Role", Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public User Role { get; set; }

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
                case "Role":
                    DeserializeUser(Role.Name);
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
                fileName = target + identity.Domain + @"\Roles\" + identity.Account + PathUtils.RoleExtension;
            }
            
            // make sure the path has the proper extension
            if (!fileName.EndsWith(PathUtils.RoleExtension, StringComparison.OrdinalIgnoreCase))
            {
                fileName += PathUtils.RoleExtension;
            }

            if (fileName.Contains("?") || fileName.Contains("*"))
            {
                var roles = System.IO.Path.GetDirectoryName(fileName);
                var domainName = System.IO.Path.GetDirectoryName(roles);
                var root = System.IO.Path.GetDirectoryName(domainName);
                foreach (var domain in Directory.EnumerateDirectories(root))
                {
                    var files = WildcardFilter(fileName, Directory.EnumerateFiles(domain + @"\Roles"), f => f).ToList();
                    foreach (var file in files)
                    {
                        DeserializeRoleFile(userName, file);
                    }
                }
            }
            else
            {
                DeserializeRoleFile(userName, fileName);
            }
        }

        private void DeserializeRoleFile(string userName, string file)
        {
            var logMessage = string.Format("Deserializing role '{0}' from '{1}'", userName, file);
            WriteVerbose(logMessage);
            WriteDebug(logMessage);
            Manager.LoadRole(file);
        }
    }
}