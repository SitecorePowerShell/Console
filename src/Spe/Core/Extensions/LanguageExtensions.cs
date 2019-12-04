using Sitecore.Configuration;
using Sitecore.Globalization;
using Spe.Core.Settings;
using Spe.Core.VersionDecoupling;

namespace Spe.Core.Extensions
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