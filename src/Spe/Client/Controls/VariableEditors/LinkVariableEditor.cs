using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web.UI;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.WebControls;
using Spe.Core.Extensions;
using Control = System.Web.UI.Control;

namespace Spe.Client.Controls.VariableEditors
{
    internal class LinkVariableEditor : IVariableEditor
    {
        public bool CanHandle(IDictionary variable, string editor, Type valueType)
        {
            return variable["Options"] != null &&
                   !string.IsNullOrEmpty(editor) && editor.HasWord("link");
        }

        public Control CreateControl(IDictionary variable, VariableEditorContext context)
        {
            var value = variable["Value"].BaseObject();
            var name = (string)variable["Name"];
            var options = VariableEditorHelper.ParseOptions(variable);

            var editorId = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("novariable_" + name + "_");
            Sitecore.Context.ClientPage.ServerProperties[editorId] = value;

            var table = new GridPanel
            {
                ID = editorId
            };

            var buttonsBorder = new Border
            {
                Class = "scContentButtons"
            };

            var linkBuilder = new List<string>();
            var linkFormat =
                @"<a href=""#"" class=""scContentButton"" onclick=""javascript:return scForm.postEvent(this,event,'{0}')"">{1}</a>";

            foreach (var option in options.Keys)
            {
                var optionName = option;
                var optionValue = options[optionName];
                linkBuilder.Add(string.Format(linkFormat, optionValue, optionName));
            }

            var links = new Literal(string.Join("&nbsp;|&nbsp;", linkBuilder.ToArray()));
            buttonsBorder.Controls.Add(links);

            var info = new Literal { Text = value.ToString(), Class = "varHint" };
            table.Controls.Add(info);
            table.Controls.Add(buttonsBorder);

            return table;
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
