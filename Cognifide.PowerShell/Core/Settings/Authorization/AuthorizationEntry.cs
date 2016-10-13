using System;
using System.Management.Automation;
using System.Xml;
using Cognifide.PowerShell.Commandlets.Security;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.ContentSearch.Linq;
using Sitecore.Security.AccessControl;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Core.Settings.Authorization
{
    public class AuthorizationEntry
    {

        public AccountIdentity Identity { get; set; }
        public AccountType IdentityType { get; set; }
        public AccessPermission AccessPermission { get; set; }

        private WildcardPattern wildcardPattern { get; set; }

        public static bool TryParse(XmlNode node, out AuthorizationEntry entry)
        {
            entry = new AuthorizationEntry();
            if (node?.Attributes == null)
            {
                return false;
            }
            var accessPermissionStr = node.Attributes?["Permission"].Value;
            var accountTypeStr = node?.Attributes["IdentityType"].Value;
            var identityStr = node?.Attributes["Identity"].Value;

            AccessPermission accessPermission;
            if (!Enum.TryParse(accessPermissionStr, true, out accessPermission) ||
                accessPermission == AccessPermission.NotSet)
            {
                return false;
            }

            AccountType accountType;
            if (!Enum.TryParse(accountTypeStr, true, out accountType) || accountType == AccountType.Unknown)
            {
                return false;
            }

            AccountIdentity identity = null;
            try
            {
                identity = new AccountIdentity(identityStr, true);
            }
            catch
            {
                PowerShellLog.Error($"Invalid identity {identityStr} provided for service configuration.");
            }

            entry.AccessPermission = accessPermission;
            entry.IdentityType = accountType;
            entry.Identity = identity;
            entry.wildcardPattern = WildcardUtils.GetWildcardPattern(identity.Name);
            return true;
        }

        public bool WildcardMatch(string userName)
        {
            return wildcardPattern.IsMatch(userName);
        }


    }
}