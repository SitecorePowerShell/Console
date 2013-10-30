using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.SitecoreIntegrations.Controls
{
    public class PowerShellListView : Listview
    {
        public int CurrentPage
        {
            get
            {
                int value = GetViewStateInt("CurrentPage");
                return value < 1 ? 1 : value;
            }
            set { SetViewStateInt("CurrentPage", value); }
        }

        public string Filter
        {
            get { return GetViewStateString("Filter"); }
            set { SetViewStateString("Filter", value); }
        }

        public string ContextId
        {
            get { return GetViewStateString("ContextId"); }
            set { SetViewStateString("ContextId", value); }
        }

        public int FilteredCount
        {
            get { return GetViewStateInt("FilteredCount"); }
            set { SetViewStateInt("FilteredCount", value); }
        }

        public ShowListViewMessage Data
        {
            get { return (ShowListViewMessage) HttpContext.Current.Session[ContextId]; }
        }

        protected override void DoClick(Message message)
        {
            string source = Sitecore.Context.ClientPage.ClientRequest.Source;
            if (source.StartsWith("SortBy"))
            {
                string columnIndex = source.Substring(source.IndexOf('_') + 1);
                int colindex = MainUtil.GetInt(columnIndex, 0);
                if (colindex > 0)
                {
                    Sort(colindex);
                }
                else
                {
                    SelectAll();
                }
            }
            else
            {
                base.DoClick(message);
            }
        }

        private void SelectAll()
        {
            foreach (var item in Items)
            {
                item.Selected = !item.Selected;
            }
        }

        protected void Sort(int columnIndex)
        {
            string columnName = ColumnNames.GetKey(columnIndex);
            if (SortBy == columnName)
            {
                SortAscending = !SortAscending;
            }
            else
            {
                SortBy = columnName;
                SortAscending = true;
            }

            IOrderedEnumerable<ShowListViewCommand.SvlDataObject> sorted = SortAscending
                ? Data.Data.OrderBy(item => item.Display[columnName], ListViewComparer.Instance)
                : Data.Data.OrderByDescending(item => item.Display[columnName], ListViewComparer.Instance);
            Data.Data = sorted.ToList();
            Refresh();
        }

        public override void Refresh()
        {
            Sitecore.Context.ClientPage.ClientResponse.DisableOutput();

            Controls.Clear();
            ColumnNames.Clear();
            ColumnNames.Add("Icon", "Icon");

            if (Data == null)
            {
                return;
            }

            int pageSize = Data.PageSize;
            int offset = (CurrentPage - 1)*pageSize;
            var columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            List<ShowListViewCommand.SvlDataObject> filteredEnum = GetFilteredItems();

            FilteredCount = filteredEnum.Count();
            foreach (var result in filteredEnum.Skip(offset).Take(pageSize))
            {
                var lvi = new ListviewItem();
                Dictionary<string, string>.KeyCollection keys = result.Display.Keys;
                lvi.ID = GetUniqueID("lvi");
                lvi.Icon = keys.Contains("__icon")
                    ? result.Display["__Icon"]
                    : keys.Contains("Icon")
                        ? result.Display["Icon"]
                        : (result.Original is Item)
                            ? (result.Original as Item).Appearance.Icon
                            : "Software/32x32/graph_node.png";
                lvi.Value = result.Id.ToString(CultureInfo.InvariantCulture);
                foreach (var column in result.Display.Keys)
                {
                    columnNames.Add(column);
                    string val = result.Display[column];
                    lvi.ColumnValues.Add(column,
                        val == "False"
                            ? "<div class='unchecked'></div>"
                            : val == "True"
                                ? "<div class='checked'></div>"
                                : val);
                }
                Controls.Add(lvi);
            }
            foreach (var column in columnNames)
            {
                ColumnNames.Add(column, column);
            }

            Sitecore.Context.ClientPage.ClientResponse.EnableOutput();
            Sitecore.Context.ClientPage.ClientResponse.SetOuterHtml(ID, this);
        }

        public List<ShowListViewCommand.SvlDataObject> GetFilteredItems()
        {
            string filter = Filter;
            bool unfiltered = string.IsNullOrEmpty(filter);
            List<ShowListViewCommand.SvlDataObject> filteredEnum = unfiltered
                ? Data.Data
                : Data.Data.FindAll(p => p.Display.Values.Any(
                    value => value.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) > -1));
            return filteredEnum;
        }
    }
}