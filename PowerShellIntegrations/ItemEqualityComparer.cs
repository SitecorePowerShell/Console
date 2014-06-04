using System;
using System.Collections.Generic;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.PowerShellIntegrations
{
    public class ItemEqualityComparer : IEqualityComparer<Item>
    {
        private static readonly ItemEqualityComparer instance = new ItemEqualityComparer();

        public bool Equals(Item left, Item right)
        {
            return left.ID == right.ID && left.Version.Number == right.Version.Number &&
                   left.Language.Name == right.Language.Name;
        }

        public int GetHashCode(Item item)
        {
            return (item.Language.Name + item.Version.Number + item.ID).GetHashCode();
        }

        public static ItemEqualityComparer Instance
        {
            get
            {
                return instance;
            }
        }
    }
}
