using System.Linq;
using System.Management.Automation;
using Sitecore;
using Sitecore.Security.Accounts;
using Sitecore.Security.Serialization;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Commands.Security;
using Spe.Core.Extensions;
using Spe.Core.VersionDecoupling;

namespace Spe.Commands.Serialization
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
            var serializationManager = TypeResolver.ResolveFromCache<ISerializationManager>();
            var pathResolver = TypeResolver.ResolveFromCache<ISerializationPathResolver>();

            if (string.IsNullOrEmpty(Root) && string.IsNullOrEmpty(Path))
            {
                if (ShouldProcess(user.Name, "Serializing user"))
                {
                    var logMessage = string.Format("Serializing user '{0}'", user.Name);
                    WriteVerbose(logMessage);
                    WriteDebug(logMessage);
                    serializationManager.DumpUser(user.Name);
                    var userReference = new UserReference(user.Name);
                    WriteObject(pathResolver.GetDirectoryPath(userReference.ToString()) + pathResolver.UserExtension);
                }
            }
            else
            {
                var userReference = new UserReference(user.Name);
                var path = Path;
                if (string.IsNullOrEmpty(path))
                {
                    if (string.IsNullOrEmpty(Root))
                    {
                        path = pathResolver.GetFilePath(userReference.ToString());
                    }
                    else
                    {
                        var target = StringUtil.EnsurePostfix('\\', System.IO.Path.GetFullPath(Root));
                        path = System.IO.Path.GetFullPath(target + userReference.ToString().Replace('/', System.IO.Path.DirectorySeparatorChar));
                        if (!path.StartsWith(target, System.StringComparison.OrdinalIgnoreCase))
                        {
                            WriteError(typeof(PSSecurityException), $"User '{user.Name}' resolves outside of '{target}'.",
                                ErrorIds.FileNameInvalid, ErrorCategory.SecurityError, user);
                            return;
                        }
                    }
                    if (!System.IO.Path.HasExtension(path))
                    {
                        path += pathResolver.UserExtension;
                    }
                }

                if (ShouldProcess(user.Name, string.Format("Serializing user to '{0}'", path)))
                {
                    var logMessage = string.Format("Serializing user '{0}' to '{1}'", user.Name, path);
                    WriteVerbose(logMessage);
                    WriteDebug(logMessage);
                    serializationManager.DumpUser(path, user);
                    WriteObject(path);
                }
            }
        }
    }
}