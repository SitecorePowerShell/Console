using System;
using System.Collections.Generic;
using System.Web;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Sitecore.IO;

namespace Cognifide.PowerShell.Core.Settings
{
    public static class PredefinedVariables
    {
        public static readonly Dictionary<string, object> Variables = new Dictionary<string, object>
        {
            ["AppPath"] = HttpRuntime.AppDomainAppPath,
            ["AppVPath"] = HttpRuntime.AppDomainAppVirtualPath,
            ["tempPath"] = Environment.GetEnvironmentVariable("temp"),
            ["tmpPath"] = Environment.GetEnvironmentVariable("tmp"),
            ["SitecoreDataFolder"] = FileUtil.MapPath(Sitecore.Configuration.Settings.DataFolder),
            ["SitecoreDebugFolder"] = FileUtil.MapPath(Sitecore.Configuration.Settings.DebugFolder),
            ["SitecoreIndexFolder"] = FileUtil.MapPath(Sitecore.Configuration.Settings.IndexFolder),
            ["SitecoreLayoutFolder"] = FileUtil.MapPath(Sitecore.Configuration.Settings.LayoutFolder),
            ["SitecoreLogFolder"] = FileUtil.MapPath(Sitecore.Configuration.Settings.LogFolder),
            ["SitecoreMediaFolder"] = FileUtil.MapPath(Sitecore.Configuration.Settings.MediaFolder),
            ["SitecorePackageFolder"] = FileUtil.MapPath(Sitecore.Configuration.Settings.PackagePath),
            ["SitecoreSerializationFolder"] = FileUtil.MapPath(Sitecore.Configuration.Settings.SerializationFolder),
            ["SitecoreTempFolder"] = FileUtil.MapPath(Sitecore.Configuration.Settings.TempFolderPath),
            ["SitecoreVersion"] = SitecoreVersion.Current
        };
    }
}
