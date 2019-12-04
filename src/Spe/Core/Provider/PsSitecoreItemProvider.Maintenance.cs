using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using Sitecore.Configuration;
using Spe.Core.Diagnostics;

namespace Spe.Core.Provider
{
    public partial class PsSitecoreItemProvider
    {
        private ProviderInfo providerInfo;

        protected override ProviderInfo Start(ProviderInfo providerInfo)
        {
            try
            {
                providerInfo.Description = "Sitecore Content Provider";
                this.providerInfo = providerInfo;
                PowerShellLog.Info($"Executing {GetType().Name}.Start(providerInfo='{providerInfo.Name ?? "null"}')");
                return providerInfo;
            }
            catch (Exception ex)
            {
                PowerShellLog.Info($"Error while executing {GetType().Name}.Start(providerInfo='{providerInfo?.Name ?? "null"}')", ex);
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