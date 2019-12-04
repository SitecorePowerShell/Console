using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Reflection;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Security.AccessControl;
using Sitecore.Security.Accounts;
using Spe.Commands.Security;

namespace Spe.Core.Settings
{
    public static class TypeAccelerators
    {
        private static Dictionary<string, Type> psAccelerators = null;

        public static Dictionary<string, Type> SitecoreAccelerators = new Dictionary<string, Type>
        {
            {"Item", typeof(Item)},
            {"ID", typeof(ID)},
            {"AccountIdentity", typeof(AccountIdentity)},
            {"SearchResultItem", typeof(SearchResultItem)},
            {"Database", typeof(Database)},
            {"Account", typeof(Account)},
            {"User", typeof(User)},
            {"Role", typeof(Role)},
            {"AccessRule", typeof(AccessRule)},
            {"SitecoreVersion", typeof(VersionDecoupling.SitecoreVersion)},
            {"CurrentSitecoreVersion", typeof(VersionDecoupling.CurrentVersion)},
        };

        public static Dictionary<string,Type> AllAccelerators {
            get
            {
                if (psAccelerators == null)
                {
                    //autocomplete accelerators
                    Type accType = typeof(PSObject).Assembly.GetType("System.Management.Automation.TypeAccelerators");
                    var accMi = accType?.GetProperty("Get", BindingFlags.Public | BindingFlags.Static).GetGetMethod();
                    psAccelerators = accMi != null
                        ? new Dictionary<string, Type>((Dictionary<string, Type>) accMi.Invoke(null, null),StringComparer.OrdinalIgnoreCase)
                        : new Dictionary<string, Type>();
                }
                return psAccelerators;
            }
        }

        internal static void AddSitecoreAccelerators()
        {
            Type accType = typeof(PSObject).Assembly.GetType("System.Management.Automation.TypeAccelerators");
            MethodInfo mi = accType?.GetMethod("Add", BindingFlags.Public | BindingFlags.Static);

            foreach (var accelerator in SitecoreAccelerators)
            {
                mi.Invoke(null, new object[] { accelerator.Key, accelerator.Value });
            }
        }
    }
}
