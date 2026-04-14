using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;

namespace Spe.Client.Controls
{
    public class MultiRootTreeList : TreeList
    {
        private const string AdditionalTreeIdsKey = "MultiRoot_AdditionalTreeIds";

        protected override void OnLoad(EventArgs e)
        {
            var roots = ParseRoots(Source);

            if (roots.Count <= 1)
            {
                base.OnLoad(e);
                return;
            }

            // Use first root for standard TreeList initialization
            var originalSource = Source;
            Source = BuildSourceForRoot(originalSource, roots[0]);

            base.OnLoad(e);

            Source = originalSource;

            var viewStateId = GetViewStateString("ID");
            var primaryTree = FindControl(viewStateId + "_all") as TreeviewEx;
            if (primaryTree?.Parent == null) return;

            var treeContainer = primaryTree.Parent;
            var additionalTreeIds = new List<string>();

            for (var i = 1; i < roots.Count; i++)
            {
                var dcId = GetUniqueID("dc_");
                var treeId = GetUniqueID("tree_");

                var dataContext = new DataContext
                {
                    ID = dcId,
                    DataViewName = "Master",
                    Root = roots[i],
                    Language = Language.Parse(ItemLanguage)
                };

                if (!string.IsNullOrEmpty(DatabaseName))
                {
                    dataContext.Parameters = "databasename=" + DatabaseName;
                }

                treeContainer.Controls.Add(dataContext);

                var additionalTree = new TreeviewEx
                {
                    ID = treeId,
                    DataContext = dcId,
                    AllowDragging = false
                };

                treeContainer.Controls.Add(additionalTree);
                additionalTreeIds.Add(treeId);
            }

            SetViewStateString(AdditionalTreeIdsKey, string.Join("|", additionalTreeIds));
        }

        protected new void Add()
        {
            if (Disabled) return;

            var viewStateId = GetViewStateString("ID");
            var selectedList = FindControl(viewStateId + "_selected") as Listbox;
            Assert.IsNotNull(selectedList, typeof(Listbox));

            var selectionItem = GetTreeSelection(viewStateId + "_all");

            if (selectionItem == null)
            {
                var additionalIds = GetViewStateString(AdditionalTreeIdsKey);
                if (!string.IsNullOrEmpty(additionalIds))
                {
                    foreach (var treeId in additionalIds.Split('|'))
                    {
                        selectionItem = GetTreeSelection(treeId);
                        if (selectionItem != null) break;
                    }
                }
            }

            if (selectionItem == null)
            {
                SheerResponse.Alert("Select an item in the Content Tree.");
                return;
            }

            if (HasExcludeTemplate(selectionItem)) return;

            if (IsDuplicateSelection(selectionItem, selectedList))
            {
                SheerResponse.Alert("You cannot select the same item twice.");
                return;
            }

            if (!HasIncludeTemplate(selectionItem)) return;

            SheerResponse.Eval($"scForm.browser.getControl('{viewStateId}_selected').selectedIndex=-1");
            var listItem = new ListItem { ID = GetUniqueID("L") };
            Sitecore.Context.ClientPage.AddControl(selectedList, listItem);
            listItem.Header = GetHeaderValue(selectionItem);
            listItem.Value = listItem.ID + "|" + selectionItem.ID;
            SheerResponse.Refresh(selectedList);
            SetModified();
        }

        private Item GetTreeSelection(string treeControlId)
        {
            var tree = FindControl(treeControlId) as TreeviewEx;
            return tree?.GetSelectionItem(Language.Parse(ItemLanguage), Sitecore.Data.Version.Latest);
        }

        private bool HasExcludeTemplate(Item item)
        {
            return item == null || HasItemTemplate(item, ExcludeTemplatesForSelection);
        }

        private bool HasIncludeTemplate(Item item)
        {
            Assert.ArgumentNotNull(item, nameof(item));
            return IncludeTemplatesForSelection.Length == 0 || HasItemTemplate(item, IncludeTemplatesForSelection);
        }

        private bool IsDuplicateSelection(Item item, Listbox listbox)
        {
            Assert.ArgumentNotNull(listbox, nameof(listbox));
            if (item == null) return true;
            if (AllowMultipleSelection) return false;

            foreach (System.Web.UI.Control control in listbox.Controls)
            {
                var strArray = control is ListItem li ? li.Value.Split('|') : new string[0];
                if (strArray.Length >= 2 && strArray[1] == item.ID.ToString())
                    return true;
            }

            return false;
        }

        private static bool HasItemTemplate(Item item, string templateList)
        {
            Assert.ArgumentNotNull(templateList, nameof(templateList));
            if (item == null || templateList.Length == 0) return false;

            var templateOptions = templateList.Split(',').Select(t => t.Trim().ToLowerInvariant()).ToList();

            if (templateOptions.Contains(item.TemplateName.Trim().ToLowerInvariant()))
                return true;

            var template = TemplateManager.GetTemplate(item);
            var baseTemplates = template.GetBaseTemplates().Select(t => t.Name.Trim().ToLowerInvariant()).ToList();

            foreach (var value in templateOptions)
            {
                if (Sitecore.Data.ID.IsID(value) && template.DescendsFromOrEquals(Sitecore.Data.ID.Parse(value)))
                    return true;
                if (baseTemplates.Contains(value))
                    return true;
            }

            return false;
        }

        internal static List<string> ParseRoots(string source)
        {
            if (string.IsNullOrEmpty(source)) return new List<string> { "/sitecore" };

            var dataSource = source.Contains("DataSource=")
                ? Sitecore.StringUtil.ExtractParameter("DataSource", source)
                : source;

            if (string.IsNullOrEmpty(dataSource)) return new List<string> { "/sitecore" };

            return dataSource.Split('|').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        }

        private static string BuildSourceForRoot(string source, string root)
        {
            if (!source.Contains("DataSource=")) return "DataSource=" + root;

            var currentDataSource = Sitecore.StringUtil.ExtractParameter("DataSource", source);
            return source.Replace("DataSource=" + currentDataSource, "DataSource=" + root);
        }
    }
}
