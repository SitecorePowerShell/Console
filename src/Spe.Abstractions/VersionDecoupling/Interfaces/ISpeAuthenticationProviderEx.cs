namespace Spe.Abstractions.VersionDecoupling.Interfaces
{
    public interface ISpeAuthenticationProviderEx : ISpeAuthenticationProvider
    {
        bool Validate(string token, string authority, out string username, out TokenValidationResult result);
    }
}
