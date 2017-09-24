using System.Linq;
using System.Management.Automation.Provider;
using System.Management.Automation.Runspaces;
using Cognifide.PowerShell.Core.Extensions;

namespace Cognifide.PowerShell.Core.Provider
{
    public class PsSitecoreItemProviderFactory
    {
        public static void AppendToSessionState(InitialSessionState state)
        {
            if (!NativeDepthSupport)
            {
                AddProvider4(state);
            }
            else
            {
                AddProvider5(state);
            }
        }

        private static bool NativeDepthSupport { get; } = typeof(ContainerCmdletProvider).GetMethodsBySig("GetChildItems", typeof(void), typeof(string),
            typeof(bool), typeof(uint)).Any();

        private static void AddProvider4(InitialSessionState state)
        {
            state.Providers.Add(new SessionStateProviderEntry("Sitecore",
                typeof(PsSitecoreItemProvider),
                string.Empty));
        }

        private static void AddProvider5(InitialSessionState state)
        {
            state.Providers.Add(new SessionStateProviderEntry("Sitecore",
                typeof(PsSitecoreItemProvider5),
                string.Empty));
        }

    }
}