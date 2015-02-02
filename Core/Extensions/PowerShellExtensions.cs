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

        public static List<T> BaseList<T>(this object enumarable) where T : class
        {
            var newList = new List<T>();
            if (enumarable is IEnumerable)
            {
                foreach (var val in enumarable as IEnumerable)
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
    }
}