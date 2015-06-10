using System.Collections.Generic;
using System.Xml;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data.Fields;
using Sitecore.Diagnostics;
using Sitecore.Web.UI.WebControls;

namespace Cognifide.PowerShell.Core.Extensions
{
    public static class FieldExtensions
    {
        public static string Render(this Field field, string parameters)
        {
            Assert.ArgumentNotNull(field, "field");

            if (string.IsNullOrEmpty(field.Value))
            {
                return string.Empty;
            }

            if (!ShouldRender(field))
            {
                return field.Value;
            }

            return FieldRenderer.Render(
              field.Item,
              field.ID.ToString(),
              parameters);
        }

        public static string Render(this Field field)
        {
            Assert.ArgumentNotNull(field, "field");
            return Render(field,string.Empty);
        }

        public static string Render(this Field field, IDictionary<string, string> parameters)
        {
            Assert.ArgumentNotNull(field, "field");
            string pass = string.Empty;

            if (parameters != null && parameters.Count > 0)
            {
                foreach (string key in parameters.Keys)
                {
                    if (pass != string.Empty)
                    {
                        pass += "&";
                    }

                    string value = parameters[key];

                    // TODO: special characters may requuire encoding
                    pass += key + "=" + value;
                }
            }

            return Render(field, pass);
        }

        static Dictionary<string,bool> canRenderType = new Dictionary<string, bool>();

        public static bool ShouldRender(Field field)
        {
            Assert.ArgumentNotNull(field, "field");

            if (string.IsNullOrEmpty(field.Value))
            {
                return false;
            }

            if (!canRenderType.ContainsKey(field.Type))
            {
                XmlNode node = Factory.GetConfigNode(
                    "fieldTypes/fieldType[@name='" + field.Type + "']");
                canRenderType[field.Type] =
                    node == null
                    || node.Attributes == null
                    || node.Attributes["render"] == null
                    || MainUtil.GetBool(node.Attributes["render"].Value, true);
            }
            return canRenderType[field.Type];
        }
    }
}