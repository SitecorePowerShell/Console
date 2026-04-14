using System;
using System.Collections;
using System.Web.UI;
using Sitecore;
using Sitecore.Text;
using Sitecore.Web.UI.HtmlControls;
using Spe.Core.Extensions;
using Control = System.Web.UI.Control;

namespace Spe.Client.Controls.VariableEditors
{
    internal class RuleVariableEditor : IVariableEditor
    {
        public bool CanHandle(IDictionary variable, string editor, Type valueType)
        {
            return !string.IsNullOrEmpty(editor) && editor.HasWord("rule");
        }

        public Control CreateControl(IDictionary variable, VariableEditorContext context)
        {
            var value = variable["Value"].BaseObject();
            var name = (string)variable["Name"];
            var editor = variable["Editor"] as string;

            var editorId = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_");
            Sitecore.Context.ClientPage.ServerProperties[editorId] = value;

            var rulesBorder = new Border
            {
                Class = "rulesWrapper",
                ID = editorId
            };

            var showActions = editor.IndexOf("action", StringComparison.OrdinalIgnoreCase) > -1;
            var source = variable["Source"] as string;
            var rulesPath = string.Empty;
            if (!string.IsNullOrEmpty(source) && source.ToLowerInvariant().Contains("rulespath"))
            {
                var urlString = new UrlString(source);
                rulesPath = urlString["rulespath"];
                var hideActions = urlString["hideactions"];
                showActions = !MainUtil.GetBool(hideActions, !showActions);
            }

            var rulesEditButton = new Button
            {
                Header = Texts.PowerShellMultiValuePrompt_GetVariableEditor_Edit_rule,
                Class = "scButton edit-button rules-edit-button",
                Click = $"EditConditionClick(\"{editorId}\",\"{showActions}\", \"{rulesPath}\")"
            };

            rulesBorder.Controls.Add(rulesEditButton);

            var rule = string.IsNullOrEmpty(value as string) ? "<ruleset />" : value as string;
            var rulesRender = new Literal
            {
                ID = editorId + "_renderer",
                Text = VariableEditorHelper.GetRuleConditionsHtml(rule, showActions)
            };
            rulesRender.Class = rulesRender.Class + " varRule";
            rulesBorder.Controls.Add(rulesRender);
            return rulesBorder;
        }

        public bool CanReadValue(Control control)
        {
            return control is Border border && border.Class == "rulesWrapper";
        }

        public void ReadValue(Control control, Hashtable result, VariableEditorContext context)
        {
            result.Add("Value", Sitecore.Context.ClientPage.ServerProperties[control.ID]);
        }
    }
}
