using Sitecore;
using Sitecore.Abstractions;
using Sitecore.DependencyInjection;
using Sitecore.Security.Authentication;
using Spe.Abstractions.VersionDecoupling.Interfaces;

namespace Spe.VersionSpecific.Services
{
    public class SpeAuthenticationManager : IAuthenticationManager
    {
        public bool Login(string username, string password)
        {
            return AuthenticationManager.Login(username, password, false);
        }

        public void Logout()
        {
            ((BaseAuthenticationManager)ServiceLocator.ServiceProvider.GetService(typeof(BaseAuthenticationManager))).Logout();
        }

        public bool IsAuthenticated => Context.IsLoggedIn;
        public string CurrentUsername => Context.User.Name;
    }
}
