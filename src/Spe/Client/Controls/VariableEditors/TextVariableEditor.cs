using System;
using System.Collections;
using System.Web.UI;
using Sitecore.Web.UI.HtmlControls;
using Spe.Core.Extensions;
using Control = System.Web.UI.Control;
using Edit = Sitecore.Web.UI.HtmlControls.Edit;
using Memo = Sitecore.Web.UI.HtmlControls.Memo;

namespace Spe.Client.Controls.VariableEditors
{
    internal class TextVariableEditor : IVariableEditor
    {
        public bool CanHandle(IDictionary variable, string editor, Type valueType)
        {
            return true;
        }

        public Control CreateControl(IDictionary variable, VariableEditorContext context)
        {
            var value = variable["Value"].BaseObject();
            var editor = variable["Editor"] as string;
            var isEditorSpecified = !string.IsNullOrEmpty(editor);

            Sitecore.Web.UI.HtmlControls.Control edit;

            var placeholder = variable["Placeholder"];
            if (isEditorSpecified && editor.HasWord("pass"))
            {
                edit = new PasswordExtended();
                if (placeholder is string)
                {
                    ((PasswordExtended)edit).PlaceholderText = placeholder.ToString();
                }
                edit.Attributes["type"] = "password";
            }
            else if (isEditorSpecified && editor.HasWord("tristate"))
            {
                edit = new Sitecore.Shell.Applications.ContentEditor.Tristate();
            }
            else
            {
                edit = new EditExtended();
                if (placeholder is string)
                {
                    ((EditExtended)edit).PlaceholderText = placeholder.ToString();
                }
                if (isEditorSpecified)
                {
                    if (editor.HasWord("number"))
                    {
                        edit.Attributes["type"] = "number";
                    }
                    else if (editor.HasWord("email"))
                    {
                        edit.Attributes["type"] = "email";
                    }
                    else
                    {
                        edit.Attributes["type"] = "text";
                    }
                }
            }

            VariableEditorHelper.ApplyTextDefaults(edit, variable, value);
            return edit;
        }

        public bool CanReadValue(Control control)
        {
            return control is Edit || control is Memo;
        }

        public void ReadValue(Control control, Hashtable result, VariableEditorContext context)
        {
            VariableEditorHelper.ReadTextValue((Sitecore.Web.UI.HtmlControls.Control)control, result);
        }
    }
}
