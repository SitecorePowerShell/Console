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
            var i = 0;
            foreach (var database in Factory.GetDatabases().Where(db=> !db.ReadOnly))
            {
                i++;
                var dbName = database?.Name ?? $"sitecore{i}";
                try
                {
                    var drive = new PSDriveInfo(dbName,
                        providerInfo,
                        dbName + ":",
                        $"Sitecore '{dbName}' database.",
                        PSCredential.Empty);
                    result.Add(drive);
                }
                catch (Exception ex)
                {
                    PowerShellLog.Error($"Error while adding PowerShell drive for database {dbName}", ex);
                }
            }
            return result;
        }
    }
}