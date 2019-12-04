using System.Collections.Generic;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data.Fields;
using Sitecore.Diagnostics;
using Sitecore.Web.UI.WebControls;

namespace Spe.Core.Extensions
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

            return !ShouldRender(field) ? field.Value : FieldRenderer.Render(field.Item, field.ID.ToString(), parameters);
        }

        public static string Render(this Field field)
        {
            Assert.ArgumentNotNull(field, "field");
            return Render(field,string.Empty);
        }

        public static string Render(this Field field, IDictionary<string, string> parameters)
        {
            Assert.ArgumentNotNull(field, "field");
            var pass = string.Empty;

            if (parameters == null || parameters.Count <= 0) return Render(field, pass);

            foreach (var key in parameters.Keys)
            {
                if (pass != string.Empty)
                {
                    pass += "&";
                }

                var value = parameters[key];

                // TODO: special characters may require encoding
                pass += key + "=" + value;
            }

            return Render(field, pass);
        }

        private static readonly Dictionary<string,bool> CanRenderType = new Dictionary<string, bool>();

        public static bool ShouldRender(Field field)
        {
            Assert.ArgumentNotNull(field, "field");

            if (string.IsNullOrEmpty(field.Value))
            {
                return false;
            }

            if (CanRenderType.ContainsKey(field.Type)) return CanRenderType[field.Type];

            var node = Factory.GetConfigNode("fieldTypes/fieldType[@name='" + field.Type + "']");
            CanRenderType[field.Type] =
                node?.Attributes?["render"] == null || MainUtil.GetBool(node.Attributes["render"].Value, true);
            return CanRenderType[field.Type];
        }
    }
}