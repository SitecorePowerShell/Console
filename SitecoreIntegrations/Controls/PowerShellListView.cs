using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Cognifide.PowerShell.Commandlets.Interactive;
using Cognifide.PowerShell.Commandlets.Interactive.Messages;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.SitecoreIntegrations.Controls
{
    public class PowerShellListView : Listview
    {
        private List<BaseListViewCommand.DataObject> filteredItems;
        public int CurrentPage
        {
            get
            {
                int value = GetViewStateInt("CurrentPage");
                return value < 1 ? 1 : value;
            }
            set
            {
                int count = FilteredItems.Count;
                int pageCount = count / Data.PageSize + ((count % Data.PageSize > 0) ? 1 : 0);
                value = Math.Min(Math.Max(1, value), pageCount);
                SetViewStateInt("CurrentPage", value);
            }
        }

        public int PageCount
        {
            get
            {
                int count = FilteredItems.Count;
                return count / Data.PageSize + ((count % Data.PageSize > 0) ? 1 : 0);
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

            IOrderedEnumerable<ShowListViewCommand.DataObject> sorted = SortAscending
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

            int pageSize = Data.PageSize;
            int offset = (CurrentPage - 1)*pageSize;
            var columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var result in FilteredItems.Skip(offset).Take(pageSize))
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

        public List<BaseListViewCommand.DataObject> FilteredItems
        {
            get
            {
                if (filteredItems == null)
                {
                    string filter = Filter;
                    filteredItems = string.IsNullOrEmpty(filter)
                        ? Data.Data
                        : Data.Data.FindAll(p => p.Display.Values.Any(
                            value => value.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) > -1));
                }
                return filteredItems;
            }
        }
    }
}