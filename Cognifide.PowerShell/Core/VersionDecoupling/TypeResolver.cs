using System;
using System.Collections.Concurrent;
using System.Reflection;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Reflection;
using Sitecore.Xml;

namespace Cognifide.PowerShell.Core.VersionDecoupling
{
    public static class TypeResolver
    {
        private static readonly ConcurrentDictionary<string, object> TypeCache = new ConcurrentDictionary<string, object>();
        private static readonly ConcurrentDictionary<string, Assembly> LoadedAssemblies = new ConcurrentDictionary<string, Assembly>();

        public static T Resolve<T>(object[] parameters = null)
        {
            var xpath = "typeMappings/mapping[@name='" + typeof (T).Name + "']";
            return (T)(parameters == null ? CreateObjectFromConfig(xpath) : CreateObjectFromConfig(xpath, parameters));
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

        public static object CreateObject(Type type, object[] parameters)
        {
            Assert.ArgumentNotNull(type, nameof (type));
            Assert.ArgumentNotNull(parameters, nameof (parameters));
            var constructorInfo = ReflectionUtil.GetConstructorInfo(type, parameters);
            if (constructorInfo != null)
            {
                var obj = constructorInfo.Invoke(parameters);
                if (obj != null)
                {
                    return obj;
                }
                Log.Warn("Constructor returned null in CreateObject: " + type.FullName, typeof (TypeResolver));
            }
            else
                Log.Warn("Could not find constructor in CreateObject: " + type.FullName + ".", typeof (TypeResolver));
            return null;
        }

        private static Assembly LoadAssembly(string assembly)
        {
            return LoadedAssemblies.GetOrAdd(assembly, assemblyName => 
                Assembly.Load(assemblyName) ?? ReflectionUtil.LoadAssembly(assemblyName));
        }

        private static object CreateObject(string assembly, string className, object[] parameters)
        {
            var assembly1 = LoadAssembly(assembly);
            if (assembly1 == null) return  null;
            var type = assembly1.GetType(className, false, true);
            return type != null ? CreateObject(type, parameters) : null;
        }

        private static object CreateObject(string typeName, object[] parameters)
        {
            if (string.IsNullOrEmpty(typeName))
                return (object) null;
            var length = typeName.IndexOf(',');
            if (length >= 0)
            {
                var className = typeName.Substring(0, length).Trim();
                return CreateObject(typeName.Substring(length + 1).Trim(), className, parameters);
            }
            var type1 = Type.GetType(typeName, false, true);
            if (type1 != null)
            {
                return CreateObject(type1, parameters);
            }
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type2 = assembly.GetType(typeName, false, true);
                if (type2 != null)
                {
                    return CreateObject(type2, parameters);
                }
            }
            return null;
        }

        private static object CreateObjectFromConfig(string configPath, string typeAttribute, object[] parameters)
        {
            var configNode = Factory.GetConfigNode(configPath);
            if (configNode == null)
                return null;
            string typeName;
            if (typeAttribute.Length > 0)
            {
                typeName = XmlUtil.GetAttribute(typeAttribute, configNode);
            }
            else
            {
                typeName = XmlUtil.GetAttribute("type", configNode);
                if (typeName.Length != 0) return CreateObject(typeName, parameters);
                typeName = XmlUtil.GetAttribute("class", configNode);
                var attribute1 = XmlUtil.GetAttribute("assembly", configNode);
                var attribute2 = XmlUtil.GetAttribute("namespace", configNode);
                if (attribute2.Length > 0)
                    typeName = attribute2 + "." + typeName;
                if (attribute1.Length > 0)
                    typeName = typeName + "," + attribute1;
            }
            return CreateObject(typeName, parameters);
        }

        private static object CreateObjectFromConfig(string configPath, object[] parameters)
        {
            return CreateObjectFromConfig(configPath, "", parameters);
        }

        private static object CreateObjectFromConfig(string configPath)
        {
            return CreateObjectFromConfig(configPath, "", new object []{});
        }
    }
}