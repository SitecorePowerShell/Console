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
    [Cmdlet(VerbsData.Export, "Role", SupportsShouldProcess = true)]
    [OutputType(typeof (string))]
    public class ExportRoleCommand : BaseCommand
    {
        [Alias("Name")]
        [Parameter(ParameterSetName = "Id", ValueFromPipeline = true, Mandatory = true, Position = 0)]
        [Parameter(ParameterSetName = "Path Id", ValueFromPipeline = true, Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public AccountIdentity Identity { get; set; }

        [Parameter(ParameterSetName = "Filter", Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Filter { get; set; }

        [Parameter(ParameterSetName = "Role", Mandatory = true, ValueFromPipeline = true, Position = 0)]
        [Parameter(ParameterSetName = "Path Role", Mandatory = true, ValueFromPipeline = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public Role Role { get; set; }

        [Parameter(ParameterSetName = "Path Id", Mandatory = true)]
        [Parameter(ParameterSetName = "Path Role", Mandatory = true)]
        [Alias("FullName", "FileName")]
        public string Path { get; set; }

        [Alias("Target")]
        [Parameter(ParameterSetName = "Id")]
        [Parameter(ParameterSetName = "Filter")]
        [Parameter(ParameterSetName = "Role")]
        public string Root { get; set; }

        protected override void ProcessRecord()
        {
            switch (ParameterSetName)
            {
                case "Role":
                case "Path Role":
                    SerializeRole(Role);
                    break;
                case "Filter":
                    var filter = Filter;
                    if (!filter.Contains("?") && !filter.Contains("*")) return;

                    var managedRoles = Context.User.Delegation.GetManagedRoles(true);
                    var roles = WildcardFilter(filter, managedRoles, role => role.Name);
                    roles.ToList().ForEach(SerializeRole);
                    break;
                default:
                    if (!this.CanFindAccount(Identity, AccountType.Role)) return;
                    SerializeRole(Role.FromName(Identity.Name));
                    break;
            }
        }

        private void SerializeRole(Role role)
        {
            if (string.IsNullOrEmpty(Root) && string.IsNullOrEmpty(Path))
            {
                if (ShouldProcess(role.Name, string.Format("Serializing role")))
                {
                    var logMessage = string.Format("Serializing role '{0}'", role.Name);
                    WriteVerbose(logMessage);
                    WriteDebug(logMessage);
                    Manager.DumpRole(role.Name);
                    WriteObject(PathUtils.GetFilePath(new RoleReference(role.Name)));
                }
            }
            else
            {
                if (string.IsNullOrEmpty(Path))
                {
                    if (string.IsNullOrEmpty(Root))
                    {
                        var roleReference = new RoleReference(role.Name);
                        Path = PathUtils.GetFilePath(roleReference);
                    }
                    else
                    {
                        var roleReference = new RoleReference(role.Name);
                        var target = Root.EndsWith("\\") ? Root : Root + "\\";
                        Path = (target + roleReference).Replace('/', System.IO.Path.DirectorySeparatorChar);
                    }
                    if (!System.IO.Path.HasExtension(Path))
                    {
                        Path += PathUtils.RoleExtension;
                    }
                }
                if (ShouldProcess(role.Name, string.Format("Serializing role to '{0}'", Path)))
                {
                    var logMessage = string.Format("Serializing role '{0}' to '{1}'", role.Name, Path);
                    WriteVerbose(logMessage);
                    WriteDebug(logMessage);
                    Manager.DumpRole(Path, role);
                    WriteObject(Path);
                }
            }
        }
    }
}