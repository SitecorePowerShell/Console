using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Web.UI;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Reflection;
using Sitecore.Resources;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Web.UI.HtmlControls;
using Spe.Core.Extensions;
using Control = System.Web.UI.Control;
using Image = Sitecore.Shell.Applications.ContentEditor.Image;

namespace Spe.Client.Controls.VariableEditors
{
    // Edits a field value (XML in and out) through the Content Editor control of a LinkBase field type such as Image or General Link.
    internal class FieldControlVariableEditor : IVariableEditor
    {
        private readonly string[] _editorWords;
        private readonly string _fieldTypePath;
        private readonly string _wrapperClass;
        private readonly Func<LinkBase> _defaultControl;

        public static FieldControlVariableEditor Image() =>
            new FieldControlVariableEditor(new[] { "image" }, "/sitecore/system/Field types/Simple Types/Image",
                "imageWrapper", () => new Image());

        public static FieldControlVariableEditor GeneralLink() =>
            new FieldControlVariableEditor(new[] { "generallink", "general link" },
                "/sitecore/system/Field types/Link Types/General Link", "linkWrapper", () => new Link());

        private FieldControlVariableEditor(string[] editorWords, string fieldTypePath, string wrapperClass,
            Func<LinkBase> defaultControl)
        {
            _editorWords = editorWords;
            _fieldTypePath = fieldTypePath;
            _wrapperClass = wrapperClass;
            _defaultControl = defaultControl;
        }

        public bool CanHandle(IDictionary variable, string editor, Type valueType)
        {
            return !string.IsNullOrEmpty(editor) && editor.HasWord(_editorWords);
        }

        public Control CreateControl(IDictionary variable, VariableEditorContext context)
        {
            var value = variable["Value"].BaseObject();
            var name = (string)variable["Name"];

            var wrapper = new Border
            {
                ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                Class = _wrapperClass
            };

            var fieldType = Sitecore.Client.CoreDatabase.GetItem(_fieldTypePath);
            var field = CreateFieldControl(fieldType);
            field.ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("field_");
            field.ItemLanguage = Context.Language.Name;
            field.Source = variable["Source"] as string ?? string.Empty;
            field.SetValue(value as string ?? string.Empty);

            wrapper.Controls.Add(new Literal(GetMenuButtons(fieldType, field.ID)));
            wrapper.Controls.Add(field);
            return wrapper;
        }

        // Same control Content Editor uses (EditorFormatter.GetEditor), so replacements such as the DAM connector's handle their own menu messages.
        private LinkBase CreateFieldControl(Item fieldType)
        {
            var control = string.IsNullOrEmpty(fieldType?["Control"]) ? null : Resource.GetWebControl(fieldType["Control"]);
            if (control == null && !string.IsNullOrEmpty(fieldType?["Assembly"]) && !string.IsNullOrEmpty(fieldType["Class"]))
            {
                control = ReflectionUtil.CreateObject(fieldType["Assembly"], fieldType["Class"], new object[0]) as Control;
            }
            return control as LinkBase ?? _defaultControl();
        }

        // Same buttons the Field Editor shows for the field type (EditorFormatter.RenderMenuButtons).
        private static string GetMenuButtons(Item fieldType, string fieldId)
        {
            var menu = fieldType?.Children["Menu"];
            var html = new StringBuilder("<div class=\"scContentButtons\">");
            foreach (var button in menu?.Children.Where(b => MainUtil.GetBool(b["Show In Field Editor"], false)) ??
                                   Enumerable.Empty<Item>())
            {
                var click = Context.ClientPage.GetClientEvent(button["Message"]).Replace("$Target", fieldId);
                html.Append($"<a href=\"#\" class=\"scContentButton\" onclick=\"{click}\">{button["Display Name"]}</a>");
            }
            return html.Append("</div>").ToString();
        }

        public bool CanReadValue(Control control)
        {
            return control is Border border && border.Class == _wrapperClass;
        }

        public void ReadValue(Control control, Hashtable result, VariableEditorContext context)
        {
            result.Add("Value", control.Controls.OfType<LinkBase>().First().GetValue());
        }
    }
}
