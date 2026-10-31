using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Web.UI;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Reflection;
using Sitecore.Resources;
using Sitecore.Web.UI.HtmlControls;
using Spe.Core.Extensions;
using Control = System.Web.UI.Control;
using Image = Sitecore.Shell.Applications.ContentEditor.Image;

namespace Spe.Client.Controls.VariableEditors
{
    internal class ImageVariableEditor : IVariableEditor
    {
        private const string ImageFieldTypePath = "/sitecore/system/Field types/Simple Types/Image";

        public bool CanHandle(IDictionary variable, string editor, Type valueType)
        {
            return !string.IsNullOrEmpty(editor) && editor.HasWord("image");
        }

        public Control CreateControl(IDictionary variable, VariableEditorContext context)
        {
            var value = variable["Value"].BaseObject();
            var name = (string)variable["Name"];

            var wrapper = new Border
            {
                ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                Class = "imageWrapper"
            };

            var fieldType = Sitecore.Client.CoreDatabase.GetItem(ImageFieldTypePath);
            var image = CreateImageControl(fieldType);
            image.ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("image_");
            image.ItemLanguage = Context.Language.Name;
            image.Source = variable["Source"] as string ?? string.Empty;
            image.SetValue(value as string ?? string.Empty);

            wrapper.Controls.Add(new Literal(GetMenuButtons(fieldType, image.ID)));
            wrapper.Controls.Add(image);
            return wrapper;
        }

        // Same control Content Editor uses (EditorFormatter.GetEditor), so replacements such as the DAM connector's handle their own menu messages.
        private static Image CreateImageControl(Item fieldType)
        {
            var control = string.IsNullOrEmpty(fieldType?["Control"]) ? null : Resource.GetWebControl(fieldType["Control"]);
            if (control == null && !string.IsNullOrEmpty(fieldType?["Assembly"]) && !string.IsNullOrEmpty(fieldType["Class"]))
            {
                control = ReflectionUtil.CreateObject(fieldType["Assembly"], fieldType["Class"], new object[0]) as Control;
            }
            return control as Image ?? new Image();
        }

        // Same buttons the Field Editor shows for an Image field (EditorFormatter.RenderMenuButtons).
        private static string GetMenuButtons(Item fieldType, string imageId)
        {
            var menu = fieldType?.Children["Menu"];
            var html = new StringBuilder("<div class=\"scContentButtons\">");
            foreach (var button in menu?.Children.Where(b => MainUtil.GetBool(b["Show In Field Editor"], false)) ??
                                   Enumerable.Empty<Item>())
            {
                var click = Context.ClientPage.GetClientEvent(button["Message"]).Replace("$Target", imageId);
                html.Append($"<a href=\"#\" class=\"scContentButton\" onclick=\"{click}\">{button["Display Name"]}</a>");
            }
            return html.Append("</div>").ToString();
        }

        public bool CanReadValue(Control control)
        {
            return control is Border border && border.Class == "imageWrapper";
        }

        public void ReadValue(Control control, Hashtable result, VariableEditorContext context)
        {
            result.Add("Value", control.Controls.OfType<Image>().First().GetValue());
        }
    }
}
