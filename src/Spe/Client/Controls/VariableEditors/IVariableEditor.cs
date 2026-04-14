using System;
using System.Collections;
using System.Web.UI;

namespace Spe.Client.Controls.VariableEditors
{
    internal interface IVariableEditor
    {
        bool CanHandle(IDictionary variable, string editor, Type valueType);

        Control CreateControl(IDictionary variable, VariableEditorContext context);

        bool CanReadValue(Control control);

        void ReadValue(Control control, Hashtable result, VariableEditorContext context);
    }
}
