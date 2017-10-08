using Cognifide.PowerShell.Core.VersionDecoupling;
using Sitecore.Buckets.FieldTypes;

namespace Cognifide.PowerShell.Client.Controls
{
    internal class BucketListExtended : BucketList
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
