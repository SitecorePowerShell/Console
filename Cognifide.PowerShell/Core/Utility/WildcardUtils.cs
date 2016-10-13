using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Cognifide.PowerShell.Core.Utility
{
    public static class WildcardUtils
    {
        public static WildcardPattern GetWildcardPattern(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                name = "*";
            }
            const WildcardOptions options = WildcardOptions.IgnoreCase | WildcardOptions.Compiled;
            var wildcard = new WildcardPattern(name, options);
            return wildcard;
        }
        public static IEnumerable<T> WildcardFilter<T>(string filter, IEnumerable<T> items,
            Func<T, string> propertyName)
        {
            var wildcardPattern = GetWildcardPattern(filter);
            return items.Where(item => wildcardPattern.IsMatch(propertyName(item)));
        }

        public static IEnumerable<T> WildcardFilterMany<T>(string[] filters, IEnumerable<T> items,
            Func<T, string> propertyName)
        {
            var matchingItems = new Dictionary<string, T>();
            var itemsList = items.ToList();
            foreach (var matchingItem in filters.SelectMany(filter => WildcardFilter(filter, itemsList, propertyName)))
            {
                matchingItems[propertyName(matchingItem)] = matchingItem;
            }
            return matchingItems.Values;
        }



    }
}