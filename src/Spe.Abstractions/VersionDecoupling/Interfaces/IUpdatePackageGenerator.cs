namespace Spe.Abstractions.VersionDecoupling.Interfaces
{
    public interface IUpdatePackageGenerator
    {
        void GeneratePackage(object diff, string licenseFileName, string fileName);
    }
}
