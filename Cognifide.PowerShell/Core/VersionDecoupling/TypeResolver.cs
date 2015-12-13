using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Cognifide.PowerShell.Core.VersionDecoupling
{
    public class TypeResolver
    {
        public static T Resolve<T>(object[] parameters = null)
        {
            var xpath = "typeMappings/mapping[@name='" + typeof (T).Name + "']";
            return (T)(parameters == null ?
              Sitecore.Reflection.ReflectionUtil.CreateObjectFromConfig(xpath) :
              Sitecore.Reflection.ReflectionUtil.CreateObjectFromConfig(xpath, parameters));
        }
    }
}