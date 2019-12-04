using System;
using System.Collections.Generic;
using System.Linq;

namespace Spe.Integrations.Install
{
    public class AttributesContainer
    {
        private Dictionary<string, object> Attributes { get; }

        public AttributesContainer()
        {
            Attributes = new Dictionary<string, object>();
        }

        public AttributesContainer(string packageAttributesString)
        {
            Attributes = new Dictionary<string, object>();
            string[] attributes = packageAttributesString.Split('|');

            foreach (var attribute in attributes.Where(x => !String.IsNullOrEmpty(x)))
            {
                string[] a = attribute.Split('=');
                Add(a[0], a[1]);
            }
        }

        public void Add(string key, string value)
        {
            Attributes.Add(key, value);
        }

        public object Get(string key)
        {
            key = key.ToLower();
            if (Attributes.ContainsKey(key))
            {
                return Attributes[key];
            }
            return null;
        }

        public bool Remove(string key)
        {
            return Attributes.Remove(key);
        }

        public string ConvertoToPackageAttributes()
        {
            var list = Attributes.Select(attribute => $"{attribute.Key}={attribute.Value}");
            return String.Join("|", list);
        }
    }
}