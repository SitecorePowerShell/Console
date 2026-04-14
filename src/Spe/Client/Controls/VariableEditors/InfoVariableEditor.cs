using System;
using System.Collections;
using System.Web.UI;
using Sitecore.Web.UI.HtmlControls;
using Spe.Core.Extensions;
using Control = System.Web.UI.Control;

namespace Spe.Client.Controls.VariableEditors
{
    internal class InfoVariableEditor : IVariableEditor
    {
        public bool CanHandle(IDictionary variable, string editor, Type valueType)
        {
            return !string.IsNullOrEmpty(editor) && editor.HasWord("info");
        }

        public Control CreateControl(IDictionary variable, VariableEditorContext context)
        {
            var value = variable["Value"].BaseObject();
            return new Literal { Text = value.ToString(), Class = "varHint" };
        }

        public bool CanReadValue(Control control)
        {
            return false;
        }

        public void ReadValue(Control control, Hashtable result, VariableEditorContext context)
        {
        }
    }
}
