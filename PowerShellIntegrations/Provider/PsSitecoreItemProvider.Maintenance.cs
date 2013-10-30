using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Sitecore.Configuration;
using Sitecore.Diagnostics;

namespace Cognifide.PowerShell.PowerShellIntegrations.Provider
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
            Log.Error(string.Format(format, args), ex);
        }

        private void LogInfo(string format, params object[] args)
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

            foreach (var database in Factory.GetDatabases())
            {
                var drive = new PSDriveInfo(database.Name,
                    providerInfo,
                    database.Name + ":", //"\\sitecore\\",
                    String.Format("Sitecore '{0}' database.", database.Name),
                    PSCredential.Empty);
                result.Add(drive);
            }

            return result;
        }

        internal static void AppendToRunSpace(RunspaceConfiguration runspaceConfiguration)
        {
            runspaceConfiguration.Providers.Append(new ProviderConfigurationEntry("CmsItemProvider",
                typeof (PsSitecoreItemProvider),
                String.Empty));
        }
    }
}