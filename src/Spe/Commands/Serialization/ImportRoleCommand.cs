using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using Sitecore.Security.Accounts;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Commands.Security;
using Spe.Core.VersionDecoupling;

namespace Spe.Commands.Serialization
{
    [Cmdlet(VerbsData.Import, "Role", SupportsShouldProcess = true)]
    public class ImportRoleCommand : BaseCommand
    {
        [Alias("Name")]
        [Parameter(ParameterSetName = "Id", ValueFromPipeline = true, Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public AccountIdentity Identity { get; set; }

        [Parameter(ParameterSetName = "Filter", Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Filter { get; set; }

        [Parameter(ParameterSetName = "Role", Mandatory = true, ValueFromPipeline = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public User Role { get; set; }

        [Parameter(ParameterSetName = "Path", Mandatory = true)]
        [Alias("FullName", "FileName")]
        public string Path { get; set; }

        [Parameter(ParameterSetName = "Id")]
        [Parameter(ParameterSetName = "Filter")]
        [Parameter(ParameterSetName = "Role")]
        public string Root { get; set; }

        protected override void ProcessRecord()
        {
            if (string.IsNullOrEmpty(Root))
            {
                Root = TypeResolver.ResolveFromCache<ISerializationPathResolver>().Root + "security\\";
            }
            switch (ParameterSetName)
            {
                case "Role":
                    DeserializeRole(Role.Name);
                    break;
                case "Filter":
                    var filter = Filter;
                    if (!filter.Contains("?") && !filter.Contains("*")) return;
                    DeserializeRole(filter);
                    break;
                case "Path":
                    DeserializeRole(Path);
                    break;
                default:
                    DeserializeRole(Identity.Name);
                    break;
            }
        }

        private void DeserializeRole(string roleName)
        {
            var fileName = roleName;

            if (fileName.Contains("?") || fileName.Contains("*"))
            {
                string rootFolder = System.IO.Path.GetDirectoryName(fileName);
                var identity = new AccountIdentity(roleName, true);

                // if path is not absolute - add the Root folder
                if (!System.IO.Path.IsPathRooted(fileName))
                {
                    rootFolder = string.IsNullOrEmpty(Root) || Root.EndsWith("\\") ? Root : Root + "\\";
                }
                
                foreach (var domain in WildcardFilter(identity.Domain, Directory.EnumerateDirectories(rootFolder), d => System.IO.Path.GetFileName(d)))
                {
                    var files = WildcardFilter(identity.Account, Directory.EnumerateFiles(domain + @"\Roles"), f => System.IO.Path.GetFileName(f)).ToList();

                    foreach (var file in files)
                    {
                        DeserializeRoleFile(identity.ToString(), file);
                    }
                }
            }
            else
            {
                // if path is not absolute - add the Root folder
                if (!System.IO.Path.IsPathRooted(fileName))
                {
                    var identity = new AccountIdentity(roleName);
                    var target = string.IsNullOrEmpty(Root) || Root.EndsWith("\\") ? Root : Root + "\\";
                    fileName = target + identity.Domain + @"\Roles\" + identity.Account + TypeResolver.ResolveFromCache<ISerializationPathResolver>().RoleExtension;
                }

                // make sure the path has the proper extension
                var roleExtension = TypeResolver.ResolveFromCache<ISerializationPathResolver>().RoleExtension;
                if (!fileName.EndsWith(roleExtension, StringComparison.OrdinalIgnoreCase))
                {
                    fileName += roleExtension;
                }

                DeserializeRoleFile(roleName, fileName);
            }
        }

        private void DeserializeRoleFile(string userName, string file)
        {
            if (ShouldProcess(userName, string.Format("Deserializing role from '{0}'", file)))
            {
                var logMessage = string.Format("Deserializing role '{0}' from '{1}'", userName, file);
                WriteVerbose(logMessage);
                WriteDebug(logMessage);
                TypeResolver.ResolveFromCache<ISerializationManager>().LoadRole(file);
            }
        }
    }
}