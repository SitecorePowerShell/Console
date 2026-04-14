using System;
using System.Collections;
using System.Web.UI;
using Sitecore;
using Spe.Core.Extensions;
using Memo = Sitecore.Web.UI.HtmlControls.Memo;
using Control = System.Web.UI.Control;

namespace Spe.Client.Controls.VariableEditors
{
    internal class MemoVariableEditor : IVariableEditor
    {
        public bool CanHandle(IDictionary variable, string editor, Type valueType)
        {
            var isEditorSpecified = !string.IsNullOrEmpty(editor);
            return (isEditorSpecified && editor.HasWord("multitext")) ||
                   (variable["lines"] != null && (int)variable["lines"] > 1);
        }

        public Control CreateControl(IDictionary variable, VariableEditorContext context)
        {
            var value = variable["Value"].BaseObject();
            var lines = MainUtil.GetInt(variable["lines"], 4);

            var edit = new Memo();
            edit.Attributes.Add("rows", lines.ToString());
            var placeholder = variable["Placeholder"];
            if (placeholder is string)
            {
                edit.Attributes.Add("Placeholder", placeholder.ToString());
            }

            VariableEditorHelper.ApplyTextDefaults(edit, variable, value);
            return edit;
        }

        public bool CanReadValue(Control control)
        {
            return control is Memo;
        }

        public void ReadValue(Control control, Hashtable result, VariableEditorContext context)
        {
            VariableEditorHelper.ReadTextValue((Sitecore.Web.UI.HtmlControls.Control)control, result);
        }
    }
}
