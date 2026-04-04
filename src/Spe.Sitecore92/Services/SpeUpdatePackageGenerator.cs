// CS0618: PackageGenerator.GeneratePackage is obsolete. Suppressed intentionally -- this class
// exists to wrap the deprecated overload for Sitecore 9.2 backwards compatibility.
// See https://github.com/SitecorePowerShell/Console/issues/1433
#pragma warning disable CS0618

using Sitecore.Update;
using Sitecore.Update.Engine;
using Spe.Abstractions.VersionDecoupling.Interfaces;

namespace Spe.VersionSpecific.Services
{
    public class SpeUpdatePackageGenerator : IUpdatePackageGenerator
    {
        public void GeneratePackage(object diff, string licenseFileName, string fileName)
        {
            PackageGenerator.GeneratePackage((DiffInfo)diff, licenseFileName, fileName);
        }
    }
}
