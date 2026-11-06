using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Web.UI;
using Sitecore;
using Sitecore.Globalization;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Web.UI.HtmlControls;
using Spe.Core.Extensions;
using Control = System.Web.UI.Control;

namespace Spe.Client.Controls.VariableEditors
{
    internal class CheckboxListVariableEditor : IVariableEditor
    {
        public bool CanHandle(IDictionary variable, string editor, Type valueType)
        {
            return variable["Options"] != null &&
                   !string.IsNullOrEmpty(editor) && editor.HasWord("check");
        }

        public Control CreateControl(IDictionary variable, VariableEditorContext context)
        {
            var value = variable["Value"].BaseObject();
            var name = (string)variable["Name"];
            var options = VariableEditorHelper.ParseOptions(variable);
            var optionTooltips = VariableEditorHelper.ParseOptionTooltips(variable);

            var checkBorder = new Border
            {
                Class = "checkListWrapper",
                ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_")
            };

            var editorId = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_");
            var link =
                new Literal(
                    @"<div class='checkListActions'>" +
                    @"<a href='#' class='scContentButton' onclick=""javascript:return scForm.postEvent(this,event,'checklist:checkall(id=" +
                    editorId + @")')"">" + Translate.Text(Texts.PowerShellMultiValuePrompt_GetCheckboxControl_Select_all) + "</a> &nbsp;|&nbsp; " +
                    @"<a href='#' class='scContentButton' onclick=""javascript:return scForm.postEvent(this,event,'checklist:uncheckall(id=" +
                    editorId + @")')"">" + Translate.Text(Texts.PowerShellMultiValuePrompt_GetCheckboxControl_Unselect_all) + "</a> &nbsp;|&nbsp;" +
                    @"<a href='#' class='scContentButton' onclick=""javascript:return scForm.postEvent(this,event,'checklist:invert(id=" +
                    editorId + @")')"">" + Translate.Text(Texts.PowerShellMultiValuePrompt_GetCheckboxControl_Invert_selection) + "</a>" +
                    @"</div>");
            checkBorder.Controls.Add(link);

            var checkList = new PSCheckList
            {
                ID = editorId,
                HeaderStyle = "margin-top:20px; display:inline-block;",
                ItemID = Sitecore.ItemIDs.RootID.ToString()
            };
            checkList.SetItemLanguage(Sitecore.Context.Language.Name);

            string[] values;
            switch (value)
            {
                case string _:
                    values = value.ToString().Split('|');
                    break;
                case IEnumerable _:
                    values =
                        ((IEnumerable)value).Cast<object>()
                        .Select(s => s?.ToString() ?? "")
                        .ToArray();
                    break;
                default:
                    values = new[] { value.ToString() };
                    break;
            }

            foreach (var item in from object option in options.Keys
                                 select option.ToString()
                into optionName
                                 let optionValue = options[optionName].ToString()
                                 select new ChecklistItem
                                 {
                                     ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID(checkList.ID),
                                     Header = optionName,
                                     Value = optionValue,
                                     Checked = values.Contains(optionValue, StringComparer.OrdinalIgnoreCase)
                                 })
            {
                var optionValue = item.Value;
                if (optionTooltips.Contains(optionValue) && optionTooltips[optionValue] != null)
                {
                    var optionTitle = optionTooltips[optionValue].ToString();
                    item.ToolTip = optionTitle;
                }
                checkList.Controls.Add(item);
            }

            checkList.TrackModified = false;
            checkList.Disabled = false;
            checkBorder.Controls.Add(checkList);
            return checkBorder;
        }

        public bool CanReadValue(Control control)
        {
            return control is Border border && border.Class == "checkListWrapper";
        }

        public void ReadValue(Control control, Hashtable result, VariableEditorContext context)
        {
            var border = (Border)control;
            var checkList = border.Controls.OfType<PSCheckList>().FirstOrDefault();
            var values =
                checkList?.Controls.Cast<System.Web.UI.Control>()
                    .Where(item => item is ChecklistItem)
                    .Cast<ChecklistItem>()
                    .Where(checkItem => checkItem.Checked)
                    .Select(checkItem => checkItem.Value)
                    .ToArray();
            result.Add("Value", values);
        }
    }
}
