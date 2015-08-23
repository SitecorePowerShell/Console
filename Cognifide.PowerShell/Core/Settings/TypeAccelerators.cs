using System;
using System.Collections.Generic;
using Cognifide.PowerShell.Commandlets.Security;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Security.AccessControl;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Core.Settings
{
    public static class TypeAccelerators
    {
        public static Dictionary<string, Type> Accelerators = new Dictionary<string, Type>
        {
            {"Item", typeof(Item)},
            {"AccountIdentity", typeof(AccountIdentity)},
            {"SearchResultItem", typeof(SearchResultItem)},
            {"Database", typeof(Database)},
            {"Account", typeof(Account)},
            {"User", typeof(User)},
            {"Role", typeof(Role)},
            {"AccessRule", typeof(AccessRule)},
        };
    }
}
