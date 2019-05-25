using System;
using System.Collections.Generic;
using System.Web;
using Sitecore.IO;
using Spe.Core.VersionDecoupling;

namespace Spe.Core.Settings
{
    public static class PredefinedVariables
    {
        public static readonly Dictionary<string, object> Variables = new Dictionary<string, object>
        {
            ["AppPath"] = HttpRuntime.AppDomainAppPath,
            ["AppVPath"] = HttpRuntime.AppDomainAppVirtualPath,
            ["tempPath"] = Environment.GetEnvironmentVariable("temp"),
            ["tmpPath"] = Environment.GetEnvironmentVariable("tmp"),
            ["SitecoreDataFolder"] = MapPath(Sitecore.Configuration.Settings.DataFolder),
            ["SitecoreDebugFolder"] = MapPath(Sitecore.Configuration.Settings.DebugFolder),
            ["SitecoreIndexFolder"] = MapPath(Sitecore.Configuration.Settings.IndexFolder),
            ["SitecoreLayoutFolder"] = MapPath(Sitecore.Configuration.Settings.LayoutFolder),
            ["SitecoreLogFolder"] = MapPath(Sitecore.Configuration.Settings.LogFolder),
            ["SitecoreMediaFolder"] = MapPath(Sitecore.Configuration.Settings.MediaFolder),
            ["SitecorePackageFolder"] = MapPath(Sitecore.Configuration.Settings.PackagePath),
            ["SitecoreSerializationFolder"] = MapPath(Sitecore.Configuration.Settings.SerializationFolder),
            ["SitecoreTempFolder"] = MapPath(Sitecore.Configuration.Settings.TempFolderPath),
            ["SitecoreVersion"] = SitecoreVersion.Current
        };

        private static string MapPath(string path)
        {
            try
            {
                return FileUtil.MapPath(path);
            }
            catch
            {
                // above can fail on UNC's with scheme - return regular path in this case.
                return path;
            }
        }
    }
}
