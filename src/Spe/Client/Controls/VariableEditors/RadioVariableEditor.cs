using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Web.UI;
using Sitecore.Web.UI.HtmlControls;
using Spe.Core.Extensions;
using Control = System.Web.UI.Control;

namespace Spe.Client.Controls.VariableEditors
{
    internal class RadioVariableEditor : IVariableEditor
    {
        public bool CanHandle(IDictionary variable, string editor, Type valueType)
        {
            return variable["Options"] != null &&
                   !string.IsNullOrEmpty(editor) && editor.HasWord("radio");
        }

        public Control CreateControl(IDictionary variable, VariableEditorContext context)
        {
            var value = variable["Value"].BaseObject();
            var name = (string)variable["Name"];
            var options = VariableEditorHelper.ParseOptions(variable);
            var optionTooltips = VariableEditorHelper.ParseOptionTooltips(variable);

            var radioList = new Groupbox
            {
                ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                Class = "scRadioGroup"
            };

            foreach (var option in options.Keys)
            {
                var optionName = option;
                var optionValue = options[optionName];
                var item = new Radiobutton
                {
                    Header = optionName.ToString(),
                    Value = optionValue.ToString(),
                    ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID(radioList.ID),
                    Name = radioList.ID,
                    Checked = optionValue.ToString() == value.ToString()
                };

                if (optionTooltips.Contains(optionValue) && optionTooltips[optionValue] != null)
                {
                    var optionTitle = optionTooltips[optionValue].ToString();
                    item.ToolTip = optionTitle;
                }

                radioList.Controls.Add(item);
                radioList.Controls.Add(new Literal("<br/>"));
            }

            VariableEditorHelper.AddControlAttributes(variable, radioList);

            return radioList;
        }

        public bool CanReadValue(Control control)
        {
            return control is Groupbox && ((Sitecore.Web.UI.HtmlControls.Control)control).Class.Contains("scRadioGroup");
        }

        public void ReadValue(Control control, Hashtable result, VariableEditorContext context)
        {
            foreach (var radioItem in
                ((Sitecore.Web.UI.HtmlControls.Control)control).Controls.OfType<Radiobutton>()
                    .Where(radioItem => radioItem.Checked))
            {
                result.Add("Value", radioItem.Value);
                break;
            }
        }
    }
}
