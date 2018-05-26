using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Cognifide.PowerShell.Commandlets.Interactive;
using Cognifide.PowerShell.Commandlets.Interactive.Messages;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.StringExtensions;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Client.Controls
{
    public class PowerShellListView : Listview
    {
        private List<BaseListViewCommand.DataObject> _filteredItems;

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
                _filteredItems = null;
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
                if (_filteredItems == null)
                {
                    var filterComplete = Filter;
                    var filters = filterComplete.Split().ToList();
                    var inPhrase = false;
                    var phraseinProgress = string.Empty;
                    var phrases = new List<string>();
                    filters = filters.Where(filter =>
                    {
                        if (filter.StartsWith("\"") && !inPhrase)
                        {
                            inPhrase = !inPhrase;
                        }
                        if (inPhrase)
                        {
                           phraseinProgress += " " + filter;
                        }
                        if (filter.EndsWith("\"") && inPhrase)
                        {
                            inPhrase = !inPhrase;
                            phrases.Add(phraseinProgress.Trim('"',' ','\t'));
                            phraseinProgress = string.Empty;
                            return false;
                        }
                        return !inPhrase;
                    }).ToList();

                    if (!phraseinProgress.IsNullOrEmpty())
                    {
                        phrases.Add(phraseinProgress.Trim('"', ' ', '\t'));
                    }

                    filters.AddRange(phrases);
                    _filteredItems = string.IsNullOrEmpty(filterComplete)
                        ? Data.Data
                        : Data.Data.FindAll(p => filters.All(filter => p.Display.Values.Any(
                            value => value.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) > -1)));
                }
                return _filteredItems;
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
                            : "Office/32x32/graph_node.png";
                lvi.Value = result.Id.ToString(CultureInfo.InvariantCulture);
                foreach (var column in result.Display.Keys)
                {
                    columnNames.Add(column);
                    var val = result.Display[column];
                    switch (val)
                    {
                        case ("False"):
                            var uncheckedSource = Sitecore.Resources.Images.GetThemedImageSource("Office/16x16/delete.png");
                            val = $"<div class='unchecked'><img src='{uncheckedSource}'/></div>";
                            break;
                        case ("True"):
                            var checkedSource = Sitecore.Resources.Images.GetThemedImageSource("Office/16x16/check.png");
                            val = $"<div class='checked'><img src='{checkedSource}'/></div>";
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