using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization.Presets;
using Sitecore.Diagnostics;

namespace Cognifide.PowerShell.Core.Serialization
{
    public class SingleEntry : IncludeEntry
    {
        public new void Process(ItemCallback callback)
        {
            Assert.IsNotNull(Database, "database");
            Assert.IsNotNull(Path, "path");
            Item item = Factory.GetDatabase(Database).GetItem(Path);
            if (item != null)
            {
                callback(item);
            }
        }
    }
}