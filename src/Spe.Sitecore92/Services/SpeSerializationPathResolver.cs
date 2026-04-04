// CS0618: PathUtils is obsolete. Suppressed intentionally -- this class exists to wrap the
// deprecated Sitecore.Data.Serialization.PathUtils API for Sitecore 9.2 backwards compatibility.
// See https://github.com/SitecorePowerShell/Console/issues/1433
#pragma warning disable CS0618

using Sitecore.Data.Serialization;
using Spe.Abstractions.VersionDecoupling.Interfaces;

namespace Spe.VersionSpecific.Services
{
    public class SpeSerializationPathResolver : ISerializationPathResolver
    {
        public string GetFilePath(string reference)
        {
            return PathUtils.GetFilePath(reference);
        }

        public string GetDirectoryPath(string reference)
        {
            return PathUtils.GetDirectoryPath(reference);
        }

        public string Root => PathUtils.Root;

        public string UserExtension => PathUtils.UserExtension;

        public string RoleExtension => PathUtils.RoleExtension;
    }
}
