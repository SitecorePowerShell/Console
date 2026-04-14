using System;
using System.Collections;
using System.Web.UI;
using Spe.Core.Extensions;
using Control = System.Web.UI.Control;

namespace Spe.Client.Controls.VariableEditors
{
    internal class MarqueeVariableEditor : IVariableEditor
    {
        public bool CanHandle(IDictionary variable, string editor, Type valueType)
        {
            return !string.IsNullOrEmpty(editor) && editor.HasWord("marquee");
        }

        public Control CreateControl(IDictionary variable, VariableEditorContext context)
        {
            var value = variable["Value"].BaseObject();
            var name = (string)variable["Name"];
            return new Marquee { InnerHtml = value.ToString(), Name = name };
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
