using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using Spe.Core.Extensions;
using Control = System.Web.UI.Control;

namespace Spe.Client.Controls.VariableEditors
{
    internal class SecurityVariableEditor : IVariableEditor
    {
        public bool CanHandle(IDictionary variable, string editor, Type valueType)
        {
            return !string.IsNullOrEmpty(editor) && editor.HasWord("role", "user");
        }

        public Control CreateControl(IDictionary variable, VariableEditorContext context)
        {
            var value = variable["Value"].BaseObject();
            var name = (string)variable["Name"];
            var editor = variable["Editor"] as string;

            var showRoles = editor.HasWord("role");
            var showUsers = editor.HasWord("user");
            var multiple = editor.HasWord("multiple");

            var picker = new UserPicker();
            picker.Style.Add("float", "left");
            picker.ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_");
            picker.Class += " scContentControl textEdit clr" + value.GetType().Name;
            picker.Value = value is IEnumerable<object>
                ? string.Join("|", ((IEnumerable<object>)value).Select(x => x.ToString()).ToArray())
                : value.ToString();
            picker.ExcludeRoles = !showRoles;
            picker.ExcludeUsers = !showUsers;
            picker.DomainName = variable["Domain"] as string ?? variable["DomainName"] as string;
            picker.Multiple = multiple;
            picker.Click = "UserPickerClick(" + picker.ID + ")";

            return picker;
        }

        public bool CanReadValue(Control control)
        {
            return control is UserPicker;
        }

        public void ReadValue(Control control, Hashtable result, VariableEditorContext context)
        {
            result.Add("Value", ((Sitecore.Web.UI.HtmlControls.Control)control).Value.Split('|'));
        }
    }
}
