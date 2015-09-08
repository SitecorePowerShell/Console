using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Cognifide.PowerShell.Commandlets.Interactive;
using Cognifide.PowerShell.Commandlets.Interactive.Messages;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Client.Controls
{
    public class PowerShellListView : Listview
    {
        private List<BaseListViewCommand.DataObject> filteredItems;

        public int CurrentPage
        {
            get
            {
                var value = GetViewStateInt("CurrentPage");
                return value < 1 ? 1 : value;
            }
            set
            {
                var count = FilteredItems.Count;
                var pageCount = count/Data.PageSize + ((count%Data.PageSize > 0) ? 1 : 0);
                value = Math.Min(Math.Max(1, value), pageCount);
                SetViewStateInt("CurrentPage", value);
            }
        }

        public int PageCount
        {
            get
            {
                var count = FilteredItems.Count;
                return count/Data.PageSize + ((count%Data.PageSize > 0) ? 1 : 0);
            }
        }

        public string Filter
        {
            get { return GetViewStateString("Filter"); }
            set
            {
                SetViewStateString("Filter", value);
                filteredItems = null;
            }
        }

        public string ContextId
        {
            get { return GetViewStateString("ContextId"); }
            set { SetViewStateString("ContextId", value); }
        }

        public string SessionId
        {
            get { return GetViewStateString("SessionId"); }
            set { SetViewStateString("SessionId", value); }
        }

        public ShowListViewMessage Data => (ShowListViewMessage) HttpContext.Current.Cache[ContextId];

        public List<BaseListViewCommand.DataObject> FilteredItems
        {
            get
            {
                if (filteredItems == null)
                {
                    var filter = Filter;
                    filteredItems = string.IsNullOrEmpty(filter)
                        ? Data.Data
                        : Data.Data.FindAll(p => p.Display.Values.Any(
                            value => value.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) > -1));
                }
                return filteredItems;
            }
        }

        protected override void DoClick(Message message)
        {
            var source = Sitecore.Context.ClientPage.ClientRequest.Source;
            if (source.StartsWith("SortBy"))
            {
                var columnIndex = source.Substring(source.IndexOf('_') + 1);
                var colindex = MainUtil.GetInt(columnIndex, 0);
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
            var columnName = ColumnNames.GetKey(columnIndex);
            if (SortBy == columnName)
            {
                SortAscending = !SortAscending;
            }
            else
            {
                SortBy = columnName;
                SortAscending = true;
            }

            var sorted = SortAscending
                ? Data.Data.OrderBy(item => item.Display[columnName], ListViewComparer.Instance)
                : Data.Data.OrderByDescending(item => item.Display[columnName], ListViewComparer.Instance);
            Data.Data = sorted.ToList();
            Refresh();
        }

        /// <summary>
        ///     Raises the load event.
        /// </summary>
        /// <param name="e">
        ///     The <see cref="T:System.EventArgs" /> instance containing the event data.
        /// </param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (Sitecore.Context.ClientPage.IsEvent)
                return;
            Sitecore.Context.ClientPage.ClientResponse.Timer("keepAlive", 1000);
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

            var pageSize = Data.PageSize;
            var offset = (CurrentPage - 1)*pageSize;
            var columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var result in FilteredItems.Skip(offset).Take(pageSize))
            {
                var lvi = new ListviewItem();
                var keys = result.Display.Keys;
                lvi.ID = GetUniqueID("lvi");
                lvi.Icon = keys.Contains("__icon")
                    ? result.Display["__Icon"]
                    : keys.Contains("Icon")
                        ? result.Display["Icon"]
                        : (result.Original is Item)
                            ? ((Item) result.Original).Appearance.Icon
                            : "Software/32x32/graph_node.png";
                lvi.Value = result.Id.ToString(CultureInfo.InvariantCulture);
                foreach (var column in result.Display.Keys)
                {
                    columnNames.Add(column);
                    var val = result.Display[column];
                    switch (val)
                    {
                        case ("False"):
                            val = "<div class='unchecked'></div>";
                            break;
                        case ("True"):
                            val = "<div class='checked'></div>";
                            break;
                    }
                    lvi.ColumnValues.Add(column, val);
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
    }
}