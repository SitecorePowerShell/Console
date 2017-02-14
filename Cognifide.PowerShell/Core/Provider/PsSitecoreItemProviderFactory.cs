using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Provider;
using System.Management.Automation.Runspaces;
using System.Web;
using Cognifide.PowerShell.Core.Extensions;

namespace Cognifide.PowerShell.Core.Provider
{
    public class PsSitecoreItemProviderFactory
    {
        public static void AppendToSessionState(InitialSessionState state)
        {
            if (NativeDepthSupport)
            {
                AddProvider5(state);
            }
            else
            {
                AddProvider4(state);
            }
        }

        public static bool NativeDepthSupport { get; } = typeof(ContainerCmdletProvider).GetMethodsBySig("GetChildItems", typeof(void), typeof(string),
            typeof(bool), typeof(uint)).Any();

        private static void AddProvider4(InitialSessionState state)
        {
            state.Providers.Add(new SessionStateProviderEntry("CmsItemProvider",
                typeof(PsSitecoreItemProvider),
                string.Empty));
        }

        private static void AddProvider5(InitialSessionState state)
        {
            state.Providers.Add(new SessionStateProviderEntry("CmsItemProvider",
                typeof(PsSitecoreItemProvider5),
                string.Empty));
        }

    }
}