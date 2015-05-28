using System.Collections.Generic;
using System.Xml;
using Sitecore.Data.Serialization.Presets;

namespace Cognifide.PowerShell.Core.Serialization
{
    public static class PresetFactory
    {
        public static IList<IncludeEntry> Create(XmlNode configuration)
        {
            var list = new List<IncludeEntry>();
            foreach (XmlNode child in configuration.ChildNodes)
            {
                if (child.Name == "include")
                {
                    list.Add(CreateIncludeEntry(child));
                }
                if (child.Name == "single")
                {
                    list.Add(CreateSingleEntry(child));
                }
            }
            return list;
        }

        private static SingleEntry CreateSingleEntry(XmlNode configuration)
        {
            var singleEntry = new SingleEntry();
            if (configuration.Attributes != null)
            {
                singleEntry.Database = configuration.Attributes["database"].Value;
                singleEntry.Path = configuration.Attributes["path"].Value;
            }
            return singleEntry;
        }

        private static IncludeEntry CreateIncludeEntry(XmlNode configuration)
        {
            var includeEntry = new IncludeEntry();
            if (configuration.Attributes != null)
            {
                includeEntry.Database = configuration.Attributes["database"].Value;
                includeEntry.Path = configuration.Attributes["path"].Value;
            }
            foreach (XmlNode child in configuration.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element && child.Name == "exclude")
                {
                    includeEntry.Exclude.Add(CreateExcludeEntry(child));
                }
                if (child.NodeType == XmlNodeType.Element && child.Name == "skip")
                {
                    includeEntry.Skip.Add(CreateExcludeEntry(child));
                }
            }
            return includeEntry;
        }

        private static ExcludeEntry CreateExcludeEntry(XmlNode configuration)
        {
            var excludeEntry = new ExcludeEntry();
            if (configuration.Attributes != null && configuration.Attributes.Count > 0)
            {
                excludeEntry.Type = configuration.Attributes[0].Name;
                excludeEntry.Value = configuration.Attributes[0].Value;
            }
            foreach (XmlNode child in configuration.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element && child.Name == "include")
                {
                    excludeEntry.Include.Add(CreateIncludeEntry(child));
                }
            }
            return excludeEntry;
        }
    }
}