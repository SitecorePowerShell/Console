using Sitecore.Shell.Applications.ContentEditor;

namespace Spe.Client.Controls
{
    internal class MultilistExtended : MultilistEx
    {
        public virtual void SetLanguage(string language)
        {
            SetLanguageInternal(language);
        }

        private void SetLanguageInternal(string language)
        {
            ItemLanguage = language;
        }

    }
}
