using System;
using System.Collections.Generic;
using System.Web;
using Sitecore.Configuration;

namespace Spe.sitecore_modules.PowerShell.Services.Processors
{
    // Canonical map of the nine origin alias names accepted by the file
    // download/upload services. The PS test suite (Remoting.Download.Tests.ps1)
    // keeps an independent literal copy of this list so a drift in either
    // direction surfaces as a test failure, not as silent disagreement.
    internal static class OriginAliasFolders
    {
        private static readonly Lazy<IReadOnlyDictionary<string, Func<string>>> _map =
            new Lazy<IReadOnlyDictionary<string, Func<string>>>(BuildMap, isThreadSafe: true);

        public static bool TryGetFolder(string aliasName, out string folder)
        {
            folder = null;
            if (string.IsNullOrEmpty(aliasName)) return false;
            if (!_map.Value.TryGetValue(aliasName, out var resolver)) return false;
            folder = resolver();
            return folder != null;
        }

        private static IReadOnlyDictionary<string, Func<string>> BuildMap()
        {
            return new Dictionary<string, Func<string>>(StringComparer.Ordinal)
            {
                ["data"]          = () => Settings.DataFolder,
                ["log"]           = () => Settings.LogFolder,
                ["media"]         = () => Settings.MediaFolder,
                ["package"]       = () => Settings.PackagePath,
                ["serialization"] = () => Settings.SerializationFolder,
                ["temp"]          = () => Settings.TempFolderPath,
                ["debug"]         = () => Settings.DebugFolder,
                ["layout"]        = () => Settings.LayoutFolder,
                ["app"]           = () => HttpRuntime.AppDomainAppPath,
            };
        }
    }
}
