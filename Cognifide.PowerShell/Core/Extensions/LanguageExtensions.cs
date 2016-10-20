using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Sitecore.Configuration;
using Sitecore.Globalization;

namespace Cognifide.PowerShell.Core.Extensions
{
    public static class LanguageExtensions
    {
        public static string GetIcon(this Language language)
        {
            var db = Factory.GetDatabase(ApplicationSettings.ScriptLibraryDb);
            var icon = db != null ? language.GetIcon(db) : string.Empty;
            return !string.IsNullOrEmpty(icon)
                ? icon
                : CurrentVersion.IsAtLeast(SitecoreVersion.V80)
                    ? "Office/32x32/flag_generic.png"
                    : "Flags/32x32/flag_generic.png";

        }
    }
}