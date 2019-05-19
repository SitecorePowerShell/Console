using System.Collections.Concurrent;

namespace Cognifide.PowerShell.Core.VersionDecoupling
{
    public static class TypeResolver
    {
        private static readonly ConcurrentDictionary<string, object> TypeCache = new ConcurrentDictionary<string, object>();
        public static T Resolve<T>(object[] parameters = null)
        {
            var xpath = "typeMappings/mapping[@name='" + typeof (T).Name + "']";
            return (T)(parameters == null ?
              Sitecore.Reflection.ReflectionUtil.CreateObjectFromConfig(xpath) :
              Sitecore.Reflection.ReflectionUtil.CreateObjectFromConfig(xpath, parameters));
        }

        public static T ResolveFromCache<T>(object[] parameters = null)
        {
            var typeName = typeof(T).Name;
            if (TypeCache.ContainsKey(typeName))
            {
                return (T) TypeCache[typeName];
            }

            var resolvedT = Resolve<T>(parameters);
            TypeCache.TryAdd(typeName, resolvedT);
            return resolvedT;
        }
    }
}