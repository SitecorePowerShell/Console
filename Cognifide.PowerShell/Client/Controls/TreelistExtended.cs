using System.Collections.Generic;
using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;

namespace Cognifide.PowerShell.Client.Controls
{
    public class TreelistExtended : TreeList
    {
        protected new void Add()
        {
            if (Disabled)
                return;
            var viewStateString = GetViewStateString("ID");
            var control1 = FindControl(viewStateString + "_all") as TreeviewEx;
            Assert.IsNotNull(control1, typeof(DataTreeview));
            var control2 = FindControl(viewStateString + "_selected") as Listbox;
            Assert.IsNotNull(control2, typeof(Listbox));
            var selectionItem = control1?.GetSelectionItem(Language.Parse(ItemLanguage), Version.Latest);
            if (selectionItem == null)
            {
                SheerResponse.Alert("Select an item in the Content Tree.");
            }
            else
            {
                if (HasExcludeTemplateForSelection(selectionItem)) { return; }
                if (IsDeniedMultipleSelection(selectionItem, control2))
                {
                    SheerResponse.Alert("You cannot select the same item twice.");
                }
                else
                {
                    if (!HasIncludeTemplateForSelection(selectionItem)) { return; }
                    SheerResponse.Eval($"scForm.browser.getControl('{viewStateString}_selected').selectedIndex=-1");
                    var listItem = new ListItem {ID = GetUniqueID("L")};
                    Sitecore.Context.ClientPage.AddControl(control2, listItem);
                    listItem.Header = GetHeaderValue(selectionItem);
                    listItem.Value = listItem.ID + "|" + selectionItem.ID;
                    SheerResponse.Refresh(control2);
                    SetModified();
                }
            }
        }

        private bool HasExcludeTemplateForSelection(Item item)
        {
            return item == null || HasItemTemplate(item, ExcludeTemplatesForSelection);
        }

        private bool HasIncludeTemplateForSelection(Item item)
        {
            Assert.ArgumentNotNull(item, nameof(item));
            return IncludeTemplatesForSelection.Length == 0 || HasItemTemplate(item, IncludeTemplatesForSelection);
        }

        private bool IsDeniedMultipleSelection(Item item, Listbox listbox)
        {
            Assert.ArgumentNotNull(listbox, nameof(listbox));
            if (item == null) { return true; }

            if (AllowMultipleSelection) { return false; }

            foreach (Control control in listbox.Controls)
            {
                var strArray = control.Value.Split('|');
                if (strArray.Length >= 2 && strArray[1] == item.ID.ToString())
                    return true;
            }

            return false;
        }

        private static bool HasItemTemplate(Item item, string templateList)
        {
            Assert.ArgumentNotNull(templateList, nameof(templateList));
            if (item == null || templateList.Length == 0) { return false; }

            var templateNames = templateList.Split(',');
            var templateOptions = new List<string>();
            foreach (var templateName in templateNames)
            {
                templateOptions.Add(templateName.Trim().ToLowerInvariant());
            }

            var hasTemplate = templateOptions.Contains(item.TemplateName.Trim().ToLowerInvariant());
            if (hasTemplate) return true;

            var template = TemplateManager.GetTemplate(item);
            var baseTemplates = template.GetBaseTemplates().Select(t => t.Name.Trim().ToLowerInvariant()).ToList();
            foreach (var value in templateOptions)
            {
                if (Sitecore.Data.ID.IsID(value) && template.DescendsFromOrEquals(Sitecore.Data.ID.Parse(value)))
                {
                    return true;
                }
                if (baseTemplates.Contains(value))
                {
                    return true;
                }
            }

            return false;
        }
    }
}