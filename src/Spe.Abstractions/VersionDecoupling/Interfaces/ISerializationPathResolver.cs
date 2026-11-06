namespace Spe.Abstractions.VersionDecoupling.Interfaces
{
    public interface ISerializationPathResolver
    {
        string GetFilePath(string reference);
        string GetDirectoryPath(string reference);
        string Root { get; }
        string UserExtension { get; }
        string RoleExtension { get; }
    }
}
