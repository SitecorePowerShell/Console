using System;
using System.IO;
using System.Linq;
using System.Web.UI;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Resources;
using Sitecore.Shell.Applications.ContentManager.Galleries;
using Sitecore.Web;
using Sitecore.Web.UI;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using Sitecore.Web.UI.XmlControls;
using Spe.Core.Diagnostics;
using Spe.Core.Extensions;
using Spe.Core.Settings;
using Control = Sitecore.Web.UI.HtmlControls.Control;
using ListItem = Sitecore.Web.UI.HtmlControls.ListItem;
using Literal = Sitecore.Web.UI.HtmlControls.Literal;

namespace Spe.Client.Controls
{
    public class MruGallery : GalleryForm
    {
        // Fields
        protected DataContext ContentDataContext;
        protected TreeviewEx ContentTreeview;
        protected Combobox Databases;
        protected string InitialDatabase;
        protected Scrollbox Scripts;
        protected Edit SearchPhrase;
        protected GalleryMenu SearchResults;
        public Item ItemFromQueryString { get; set; }

        private const int DefaultPageSize = 10;

        private int PageSize
        {
            get => int.TryParse(Sitecore.StringUtil.GetString(Sitecore.Context.ClientPage.ServerProperties["MruPageSize"]),
                out var v) && v > 0 ? v : DefaultPageSize;
            set => Sitecore.Context.ClientPage.ServerProperties["MruPageSize"] = value.ToString();
        }

        private string LastPhrase
        {
            get => Sitecore.StringUtil.GetString(Sitecore.Context.ClientPage.ServerProperties["MruLastPhrase"]);
            set => Sitecore.Context.ClientPage.ServerProperties["MruLastPhrase"] = value ?? string.Empty;
        }

        protected void LoadMore()
        {
            PageSize += DefaultPageSize;
            ChangeSearchPhrase();
        }

        public override void HandleMessage(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            if (message.Name != "event:click" && message.Name != "datacontext:changed" && message.Name != "event:change" &&
                message.Name != "event:keypress")
            {
                Invoke(message, true);
            }
        }

        // Methods
        protected void ContentTreeview_Click()
        {
            var folder = ContentDataContext.GetFolder();
            if (folder.IsPowerShellScript() || folder.IsPowerShellModule())
            {
                Load(folder.Uri.ToString());
            }
        }

        protected void Load(string uri)
        {
            Assert.ArgumentNotNull(uri, "uri");
            var uri2 = ItemUri.Parse(uri);
            if (uri2 != null)
            {
                var item = Database.GetItem(uri2);
                if (item != null)
                {
                    SheerResponse.Eval(
                        string.Concat("scForm.getParentForm().invoke(\"ise:mruopen(id=", item.ID, ",language=",
                            item.Language, ",version=", item.Version, ",db=", item.Database.Name, ")\")"));
                }
            }
        }

        protected void ExecuteMruItem(string command)
        {
            Assert.ArgumentNotNull(command, "command");
            if (command != null)
            {
                SheerResponse.Eval(
                    string.Concat("scForm.getParentForm().invoke(\"", command, "\")"));
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            ItemFromQueryString = UIUtil.GetItemFromQueryString(Context.ContentDatabase);
            if (Context.ClientPage.IsEvent) return;

            ChangeSearchPhrase();

            var db = WebUtil.GetQueryString("contextDb");
            var itemId = WebUtil.GetQueryString("contextItem");
            InitialDatabase =
                string.IsNullOrEmpty(db) || "core".Equals(db, StringComparison.OrdinalIgnoreCase)
                    ? ApplicationSettings.ScriptLibraryDb
                    : db;

            BuildDatabases();

            ContentDataContext.GetFromQueryString();
            ContentDataContext.BeginUpdate();
            ContentDataContext.Parameters = $"databasename={InitialDatabase}";
            ContentTreeview.RefreshRoot();
            ContentDataContext.Root = ApplicationSettings.ScriptLibraryRoot.ID.ToString();

            if (!string.IsNullOrEmpty(itemId) && !string.IsNullOrEmpty(db))
            {
                ContentDataContext.SetFolder(Factory.GetDatabase(db).GetItem(itemId).Uri);
            }

            ContentDataContext.EndUpdate();
            ContentTreeview.RefreshRoot();

            ContentDataContext.GetFromQueryString();
            ContentDataContext.GetState(out var item, out var item2, out var itemArray);
            if (itemArray.Length > 0)
            {
                ContentDataContext.Folder = itemArray[0].ID.ToString();
            }
            var placeholderText = Translate.Text(
                Texts
                    .MruGallery_OnLoad_Script_name_to_search_for___prefix_with_e_g___master___to_narrow_to_specific_database);
            SearchPhrase.Placeholder = placeholderText;
        }

        private void BuildDatabases()
        {
            foreach (var str in Factory.GetDatabaseNames())
            {
                if (!string.Equals(str, "core", StringComparison.OrdinalIgnoreCase) &&
                    !Sitecore.Client.GetDatabaseNotNull(str).ReadOnly)
                {
                    var child = new ListItem();
                    Databases.Controls.Add(child);
                    child.ID = Control.GetUniqueID("ListItem");
                    child.Header = str;
                    child.Value = str;
                    child.Selected = str.Equals(InitialDatabase, StringComparison.InvariantCultureIgnoreCase);
                }
            }
        }

        protected void ChangeDatabase()
        {
            var name = Databases.SelectedItem.Value;
            ContentDataContext.BeginUpdate();
            ContentDataContext.Parameters = "databasename=" + name;
            ContentDataContext.EndUpdate();
            ContentTreeview.RefreshRoot();
        }

        protected void ChangeSearchPhrase()
        {
            var raw = SearchPhrase.Value ?? string.Empty;
            if (!string.Equals(raw, LastPhrase, StringComparison.Ordinal))
            {
                PageSize = DefaultPageSize;
                LastPhrase = raw;
            }
            var includeBody = raw.StartsWith("~");
            if (includeBody) raw = raw.Substring(1);
            var parts = raw.Split(':');
            var database = parts.Length > 1 ? parts[0] : string.Empty;
            var phrase = parts.Length > 1 ? parts[1] : parts[0];
            var recentHeader = new MenuHeader();
            SearchResults.Controls.AddAt(0, recentHeader);
            Scripts.Controls.Clear();
            try
            {
                if (string.IsNullOrEmpty(phrase))
                {
                    recentHeader.Header =
                        Translate.Text(Texts.MruGallery_ChangeSearchPhrase_Most_Recently_opened_scripts_);
                    var settings = ApplicationSettings.GetInstance(ApplicationNames.ISE);
                    var entries = Spe.Client.Applications.PowerShellIse.ParseMruEntries(settings.MostRecentlyUsedScripts);
                    var stale = new System.Collections.Generic.List<Spe.Client.Applications.PowerShellIse.MruEntry>();
                    foreach (var entry in entries)
                    {
                        var scriptItem = Factory.GetDatabase(entry.Db)?.GetItem(entry.Id);
                        if (scriptItem != null)
                        {
                            RenderRecent(scriptItem);
                        }
                        else
                        {
                            stale.Add(entry);
                        }
                    }
                    if (stale.Count > 0)
                    {
                        foreach (var s in stale) entries.Remove(s);
                        settings.MostRecentlyUsedScripts = Spe.Client.Applications.PowerShellIse.SerializeMruEntries(entries);
                        settings.Save();
                    }

                }
                else
                {
                    var targetDb = database.Length > 0
                        ? database
                        : Databases.SelectedItem?.Value ?? ApplicationSettings.ScriptLibraryDb;
                    recentHeader.Header =
                        Translate.Text(Texts.MruGallery_ChangeSearchPhrase_Scripts_matching____0___in___1____database,
                            phrase, targetDb);
                    var renderedCount = 0;
                    var totalCount = 0;
                    foreach (var index in ContentSearchManager.Indexes)
                    {
                        if (index.Name.StartsWith("sitecore_" + targetDb, StringComparison.OrdinalIgnoreCase) &&
                            index.Name.EndsWith("_index", StringComparison.OrdinalIgnoreCase))
                        {
                            var (rendered, total) = SearchDatabase(index.Name, phrase, includeBody, PageSize);
                            renderedCount += rendered;
                            totalCount += total;
                        }
                    }
                    if (totalCount == 0)
                    {
                        ShowScriptEnumerationProblem();
                    }
                    else
                    {
                        RenderResultsFooter(renderedCount, totalCount);
                    }
                }
            }
            catch (Exception ex)
            {
                PowerShellLog.Error("[ISE] action=showMruEntries status=failed", ex);
                ShowScriptEnumerationProblem();
            }
            var writer = new HtmlTextWriter(new StringWriter());
            SearchResults.RenderControl(writer);
            SheerResponse.SetOuterHtml(SearchResults.ID, writer.InnerWriter.ToString());
        }

        private void ShowScriptEnumerationProblem()
        {
            Context.ClientPage.AddControl(Scripts,
                new Literal
                {
                    Text = "<div class='noScript'><br/><br/>" +
                           Translate.Text(
                               Texts.MruGallery_ChangeSearchPhrase_No_scripts_found____Do_you_need_to_re_index_your_databases_) +
                           "</div>"
                });
            Scripts.CssStyle = "text-align: center;";
        }

        private (int rendered, int total) SearchDatabase(string indexName, string phrase, bool includeBody, int pageSize)
        {
            using (
                var context =
                    ContentSearchManager.GetIndex(indexName)
                        .CreateSearchContext())
            {
                var rendered = 0;
                var rootID = ApplicationSettings.ScriptLibraryRoot.ID.ToShortID().ToString().ToLower();
                var query =
                    context.GetQueryable<SearchResultItem>()
                        .Where(
                            i =>
                                i["_path"].Contains(rootID) &&
                                i["_templatename"] == "PowerShell Script");
                if (!string.IsNullOrWhiteSpace(phrase))
                {
                    query = includeBody
                        ? query.Where(i => i["_name"].Contains(phrase) || i["_content"].Contains(phrase))
                        : query.Where(i => i["_name"].Contains(phrase));
                }
                var results = query.Take(pageSize).GetResults();
                foreach (var hit in results.Hits)
                {
                    var scriptItem = hit.Document.GetItem();
                    if (scriptItem != null)
                    {
                        // Re-fetch a full item via the database to ensure all fields
                        // (DisplayName, Paths) are populated, not just the indexed subset.
                        var fullItem = scriptItem.Database.GetItem(scriptItem.ID);
                        RenderRecent(fullItem ?? scriptItem);
                        rendered++;
                    }
                }
                return (rendered, results.TotalSearchResults);
            }
        }

        private void RenderResultsFooter(int rendered, int total)
        {
            var loadMore = rendered < total
                ? " <a href=\"#\" class=\"mruLoadMore\" onclick=\"scForm.postEvent(this,event,'LoadMore');return false;\">Load more</a>"
                : string.Empty;
            Context.ClientPage.AddControl(Scripts, new Literal
            {
                Text = $"<div class=\"mruResultsFooter\">Showing {rendered} of {total}{loadMore}</div>"
            });
        }

        private void RenderRecent(Item scriptItem)
        {
            if (scriptItem == null)
            {
                return;
            }
            var control = ControlFactory.GetControl("MruGallery.SearchItem") as XmlControl;
            Assert.IsNotNull(control, typeof (XmlControl), Translate.Text(Sitecore.Texts.XML_CONTROL_0_NOT_FOUND),
                "MruGallery.SearchItem");

            Context.ClientPage.AddControl(Scripts, control);

            var iconUrl = scriptItem.Appearance.Icon;
            if (!string.IsNullOrWhiteSpace(iconUrl))
            {
                var builder = new ImageBuilder
                {
                    Src = Images.GetThemedImageSource(iconUrl, ImageDimension.id16x16),
                    Class = "scRibbonToolbarSmallGalleryButtonIcon",
                    Alt = scriptItem.DisplayName
                };
                iconUrl = builder.ToString();
            }

            var currentScript = ItemFromQueryString != null && ItemFromQueryString.ID == scriptItem.ID &&
                                ItemFromQueryString.Database.Name == scriptItem.Database.Name;

            control["ScriptIcon"] = "<div class=\"versionNum\">" + iconUrl + "</div>";
            control["Location"] = GetDisplayLocation(scriptItem.Paths.ParentPath);
            control["Database"] = scriptItem.Database.Name;
            control["Name"] = string.IsNullOrEmpty(scriptItem.DisplayName) ? scriptItem.Name : scriptItem.DisplayName;
            control["Click"] = string.Format("ExecuteMruItem(\"ise:mruopen(id={0},db={1})\")", scriptItem.ID,
                scriptItem.Database.Name);
            control["Class"] = currentScript ? "selected" : string.Empty;
        }

        private static string GetDisplayLocation(string parentPath)
        {
            if (string.IsNullOrEmpty(parentPath)) return string.Empty;
            var prefix = ApplicationSettings.ScriptLibraryPath.TrimEnd('/');
            return parentPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? parentPath.Substring(prefix.Length)
                : parentPath;
        }
    }
}