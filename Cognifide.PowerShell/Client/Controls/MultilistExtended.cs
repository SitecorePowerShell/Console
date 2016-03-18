using Cognifide.PowerShell.Core.VersionDecoupling;
using Sitecore.Shell.Applications.ContentEditor;

namespace Cognifide.PowerShell.Client.Controls
{
    internal class MultilistExtended : MultilistEx
    {
        public virtual void SetLanguage(string language)
        {
            if (VersionResolver.IsVersionHigherOrEqual(VersionResolver.SitecoreVersion71))
            {
                SetLanguageInternal(language);
            }
        }

        private void SetLanguageInternal(string language)
        {
            ItemLanguage = language;
        }

    }
}
