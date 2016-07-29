using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Configuration;
using Sitecore.Diagnostics;

namespace Cognifide.PowerShell.Core.Provider
{
    public partial class PsSitecoreItemProvider
    {
        protected override ProviderInfo Start(ProviderInfo sitecoreProviderInfo)
        {
            try
            {
                sitecoreProviderInfo.Description = "Sitcore Content Provider";
                providerInfo = sitecoreProviderInfo;
                LogInfo("Executing Start(string providerInfo='{0}')", sitecoreProviderInfo.Name);
                return sitecoreProviderInfo;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error while executing Start(string providerInfo='{0}')", sitecoreProviderInfo.Name);
                throw;
            }
        }

        private static void LogError(Exception ex, string format, params object[] args)
        {
            PowerShellLog.Error(string.Format(format, args), ex);
        }

        private static void LogInfo(string format, params object[] args)
        {
            // uncomment only for console diagnostics
            //Sitecore.Diagnostics.Log.Info(string.Format(format, args), this);
        }

        protected override void Stop()
        {
            // perform any cleanup
        }

        protected override Collection<PSDriveInfo> InitializeDefaultDrives()
        {
            var result = new Collection<PSDriveInfo>();

            foreach (var drive in Factory.GetDatabases().Select(database => new PSDriveInfo(database.Name,
                providerInfo,
                database.Name + ":", //"\\sitecore\\",
                string.Format("Sitecore '{0}' database.", database.Name),
                PSCredential.Empty)))
            {
                result.Add(drive);
            }

            return result;
        }

        internal static void AppendToRunSpace(RunspaceConfiguration runspaceConfiguration)
        {
            runspaceConfiguration.Providers.Append(new ProviderConfigurationEntry("CmsItemProvider",
                typeof (PsSitecoreItemProvider),
                string.Empty));
        }

        public static void AppendToSessionState(InitialSessionState state)
        {
            state.Providers.Add(new SessionStateProviderEntry("CmsItemProvider",
                typeof(PsSitecoreItemProvider),
                string.Empty));
        }

    }
}