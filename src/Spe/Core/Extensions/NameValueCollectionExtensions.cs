using System.Collections.Specialized;

namespace Spe.Core.Extensions
{
    public static class NameValueCollectionExtensions
    {
        public static readonly string[] ItemId = new[] { "id", "itemId" };
        public static readonly string[] ItemDb = new[] { "db", "itemDb" };
        public static readonly string[] ItemLang = new[] { "lang", "itemLang", "la", "language" };
        public static readonly string[] ItemVer = new[] { "vs", "ver", "itemVer", "version" };
        public static readonly string[] ScriptId = new[] { "scriptId", "script" };
        public static readonly string[] ScriptDb = new[] { "scriptDb" };

        public static string TryGetValue(this NameValueCollection parameters, string[] keys, string fallback)
        {
            foreach (var key in keys)
            {
                if (!string.IsNullOrWhiteSpace(parameters[key]))
                {
                    return parameters[key];
                }
            }
            return fallback;
        }

    }
}