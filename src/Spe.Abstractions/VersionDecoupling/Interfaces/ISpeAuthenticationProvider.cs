namespace Spe.Abstractions.VersionDecoupling.Interfaces
{
    public interface ISpeAuthenticationProvider
    {
        bool Validate(string token, string authority, out string username);
    }
}
