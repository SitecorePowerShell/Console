using Sitecore;
using Sitecore.Abstractions;
using Sitecore.Owin.Authentication.Identity;
using Sitecore.Security.Authentication;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Sitecore.DependencyInjection;
using Sitecore.Security.Accounts;

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

        public bool ValidateUser(string username, string password)
        {
            var membershipService = (IMembership)ServiceLocator.ServiceProvider.GetService(typeof(IMembership));

            return membershipService.ValidateUser(username, password);
        }

        public void SwitchToUser(string username, bool isAuthenticated)
        {
            var userSwitcher = new UserSwitcher(User.FromName(username, isAuthenticated));
        }
    }
}
