using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;

namespace Cognifide.PowerShell.Core.Extensions
{
    internal static class PowerShellExtensions
    {
        public static object BaseObject(this object obj)
        {
            while ((obj is PSObject))
            {
                obj = (obj as PSObject).ImmediateBaseObject;
            }
            return obj;
        }

        public static List<T> BaseList<T>(this object enumerable) where T : class
        {
            var newList = new List<T>();
            if (enumerable is IEnumerable)
            {
                foreach (var val in enumerable as IEnumerable)
                {
                    var newVal = val.BaseObject();
                    if (newVal is T)
                    {
                        newList.Add(newVal as T);
                    }
                }
            }
            return newList;
        }

        public static object[] BaseArray(this IList array)
        {
            var newArray = new object[array.Count];
            for (var i = 0; i < array.Count; i++)
            {
                newArray[i] = array[i].BaseObject();
            }
            return newArray;
        }
    }
}