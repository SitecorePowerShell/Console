using System.Linq;
using System.Web.Security;
using Sitecore.Security.Accounts;
using Sitecore.SecurityModel;

namespace Spe.Commandlets.Security.Accounts
{
    public class BaseSecurityCommand : BaseCommand
    {
        public static string[] UserNames => Membership.GetAllUsers().OfType<MembershipUser>().Select(user => $"\"{user.UserName}\"").ToArray();
        public static string[] RoleNames => RolesInRolesManager.GetAllRoles().Select(role => $"\"{role.Name}\"").ToArray();
        public static string[] RoleAndUserNames => UserNames.Concat(RoleNames).ToArray();
        public static string[] DomainNames => DomainManager.GetDomains().Select(domain => $"\"{domain.Name}\"").ToArray();
        public static readonly string[] AccountTypeNames = { "Role", "User", "All" };

    }
}