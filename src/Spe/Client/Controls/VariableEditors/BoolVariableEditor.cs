using System;
using System.Collections;
using System.Linq;
using System.Web.UI;
using Sitecore.Web.UI.HtmlControls;
using Spe.Core.Extensions;
using Checkbox = Sitecore.Web.UI.HtmlControls.Checkbox;
using Control = System.Web.UI.Control;

namespace Spe.Client.Controls.VariableEditors
{
    internal class BoolVariableEditor : IVariableEditor
    {
        public bool CanHandle(IDictionary variable, string editor, Type valueType)
        {
            return valueType == typeof(bool) ||
                   (!string.IsNullOrEmpty(editor) && editor.HasWord("bool"));
        }

        public Control CreateControl(IDictionary variable, VariableEditorContext context)
        {
            var value = variable["Value"].BaseObject();
            var name = (string)variable["Name"];

            var checkboxBorder = new Border
            {
                Class = "checkBoxWrapper",
                ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_")
            };

            var checkBox = new Checkbox
            {
                ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                Header = (string)variable["Title"],
                HeaderStyle = "display:inline-block;",
                Checked = (bool)value,
                Class = "varCheckbox"
            };

            var tip = variable["Tooltip"] as string;
            if (!string.IsNullOrEmpty(tip))
            {
                checkBox.ToolTip = tip.RemoveHtmlTags();
            }

            checkboxBorder.Controls.Add(checkBox);

            VariableEditorHelper.AddControlAttributes(variable, checkBox);

            return checkboxBorder;
        }

        public bool CanReadValue(Control control)
        {
            return control is Border border && border.Class == "checkBoxWrapper";
        }

        public void ReadValue(Control control, Hashtable result, VariableEditorContext context)
        {
            var border = (Border)control;
            foreach (var boolValue in border.Controls.OfType<Checkbox>().Select(ctl => ctl.Checked))
            {
                result.Add("Value", boolValue);
            }
        }
    }
}
