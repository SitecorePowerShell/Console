using System;
using System.Collections.Generic;

namespace Spe.Core.Extensions
{
    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (var obj in collection)
            {
                action(obj);
            }
        }
    }
}