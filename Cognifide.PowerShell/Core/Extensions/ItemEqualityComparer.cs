using System.Collections.Generic;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Core.Extensions
{
    public class ItemEqualityComparer : IEqualityComparer<Item>
    {
        public static ItemEqualityComparer Instance { get; } = new ItemEqualityComparer();

        public bool Equals(Item left, Item right)
        {
            if (left == null && right == null) return true;
            if (left == null || right == null) return false;

            return left.ID == right.ID && left.Version.Number == right.Version.Number &&
                   left.Language.Name == right.Language.Name;
        }

        public int GetHashCode(Item item)
        {
            return (item.Language.Name + item.Version.Number + item.ID).GetHashCode();
        }
    }
}