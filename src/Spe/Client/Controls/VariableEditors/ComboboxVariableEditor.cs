using System;
using System.Collections;
using System.Collections.Specialized;
using System.Web.UI;
using Sitecore.Web.UI.HtmlControls;
using Spe.Core.Extensions;
using Control = System.Web.UI.Control;

namespace Spe.Client.Controls.VariableEditors
{
    internal class ComboboxVariableEditor : IVariableEditor
    {
        public bool CanHandle(IDictionary variable, string editor, Type valueType)
        {
            return variable["Options"] != null;
        }

        public Control CreateControl(IDictionary variable, VariableEditorContext context)
        {
            var value = variable["Value"].BaseObject();
            var options = VariableEditorHelper.ParseOptions(variable);
            var optionTooltips = VariableEditorHelper.ParseOptionTooltips(variable);

            var combo = new Combobox();
            var placeholder = variable["Placeholder"];
            if (placeholder is string)
            {
                var option = new ListItem
                {
                    Header = placeholder.ToString(),
                    Value = "",
                    Selected = true
                };
                combo.Controls.Add(option);
            }

            var useTooltips = optionTooltips.Count > 0;
            foreach (var option in options.Keys)
            {
                var optionName = option;
                var optionValue = options[optionName];
                var item = new ListItem
                {
                    Header = optionName.ToString(),
                    Value = optionValue.ToString()
                };

                if (useTooltips)
                {
                    if (optionTooltips.Contains(optionValue) && optionTooltips[optionValue] != null)
                    {
                        var optionTitle = optionTooltips[optionValue].ToString();
                        item.ToolTip = optionTitle;
                    }
                }

                combo.Controls.Add(item);
            }

            VariableEditorHelper.AddControlAttributes(variable, combo);
            VariableEditorHelper.ApplyTextDefaults(combo, variable, value);

            return combo;
        }

        public bool CanReadValue(Control control)
        {
            return control is Combobox;
        }

        public void ReadValue(Control control, Hashtable result, VariableEditorContext context)
        {
            result.Add("Value", ((Combobox)control).Value);
        }
    }
}
