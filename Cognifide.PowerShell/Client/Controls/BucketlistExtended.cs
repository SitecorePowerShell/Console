using System;
using Sitecore.Buckets.FieldTypes;

namespace Cognifide.PowerShell.Client.Controls
{
    internal class BucketListExtended : BucketList
    {
        public BucketListExtended()
        {
            this.FieldID = Guid.Empty.ToString();
        }

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
