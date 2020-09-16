using Sitecore.Configuration;
using Spe.Abstractions.VersionDecoupling.Interfaces;

namespace Spe.Core.Settings.Authorization
{
    public static class ServiceAuthenticationManager
    {
        static ServiceAuthenticationManager()
        {
            AuthenticationProvider = (ISpeAuthenticationProvider)Factory.CreateObject("/sitecore/powershell/authenticationProvider", false);
        }

        public static ISpeAuthenticationProvider AuthenticationProvider { get; }
    }
}