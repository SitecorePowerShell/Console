using Sitecore.Configuration;
using Sitecore.Globalization;
using Spe.Core.Settings;

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
                : "Office/32x32/flag_generic.png";

        }
    }
}