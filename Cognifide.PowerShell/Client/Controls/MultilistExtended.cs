using Cognifide.PowerShell.Core.VersionDecoupling;
using Sitecore.Shell.Applications.ContentEditor;

namespace Cognifide.PowerShell.Client.Controls
{
    internal class MultilistExtended : MultilistEx
    {
        public virtual void SetLanguage(string language)
        {
            if (CurrentVersion.IsAtLeast(SitecoreVersion.V71))
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
