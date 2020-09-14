using System.Web.Security;
using Sitecore;
using Sitecore.Security.Accounts;
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
            Context.Logout();
        }

        public bool IsAuthenticated => Context.IsLoggedIn;
        public string CurrentUsername => Context.User.Name;

        public bool ValidateUser(string username, string password)
        {
            return Membership.ValidateUser(username, password);
        }

        public void SwitchToUser(string username, bool isAuthenticated)
        {
            var userSwitcher = new UserSwitcher(User.FromName(username, isAuthenticated));
        }
    }
}
