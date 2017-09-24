using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cognifide.PowerShell.Core.Extensions
{
    public static class TypeExtensions
    {
        public static IEnumerable<MethodInfo> GetMethodsBySig(this Type type, string name, Type returnType,
            params Type[] parameterTypes)
        {
            return type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where((m) =>
            {
                if (m.Name != name)
                {
                    return false;
                }
                if (m.ReturnType != returnType) return false;
                var parameters = m.GetParameters();
                if ((parameterTypes == null || parameterTypes.Length == 0))
                {
                    return parameters.Length == 0;
                }
                if (parameters.Length != parameterTypes.Length)
                {
                    return false;
                }
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    if (parameters[i].ParameterType != parameterTypes[i])
                        return false;
                }
                return true;
            });
        }
    }
}