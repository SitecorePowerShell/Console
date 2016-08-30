using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore;
using Sitecore.Data;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Framework;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Commandlets.Interactive.Messages
{
    [Serializable]
    public class ShowListViewMessage : IMessage
    {
        public ShowListViewMessage(List<BaseListViewCommand.DataObject> data, int pageSize, string title, string icon,
            string width, string height, bool modal, string infoTitle, string infoDescription, string sessionId,
            object actionData, Hashtable[] property, string viewName, string missingDataMessage,
            ShowListViewFeatures visibleFeatures)
        {
            Data = data;
            Title = title;
            Width = width;
            Height = height;
            PageSize = pageSize;
            Icon = icon;
            Modal = modal;
            InfoTitle = infoTitle;
            InfoDescription = infoDescription;
            SessionId = sessionId;
            ActionData = actionData;
            ViewName = viewName;
            MissingDataMessage = missingDataMessage;
            VisibleFeatures = visibleFeatures;
            Property = property;
        }


        public List<BaseListViewCommand.DataObject> Data { get; set; }
        public string Title { get; set; }
        public string Width { get; private set; }
        public string Height { get; private set; }
        public string Icon { get; private set; }
        public int PageSize { get; private set; }
        public bool Modal { get; set; }
        public string InfoTitle { get; private set; }
        public string InfoDescription { get; private set; }
        public Hashtable[] Property { get; private set; }
        public string ViewName { get; set; }
        public string MissingDataMessage { get; set; }
        public ShowListViewFeatures VisibleFeatures { get; set; }

        public string FormatProperty
        {
            get
            {
                return Property
                    .Select(p => $"@{{Label=\"{p["Label"]}\";Expression={{{p["Expression"]}}}}},")
                    .Aggregate((a, b) => a + b)
                    .TrimEnd(',');
            }
        }

        public string ExportProperty
        {
            get
            {
                return Property
                    .Where(p =>
                    {
                        var label = p["Label"].ToString().ToLower();
                        return label != "icon" && label != "__icon";
                    })
                    .Select(p => $"'{p["Label"]}',")
                    .Aggregate((a, b) => a + b)
                    .TrimEnd(',');
            }
        }

        public string SessionId { get; private set; }
        public object ActionData { get; private set; }

        public void Execute()
        {
            var resultSig = Guid.NewGuid().ToString();
            HttpContext.Current.Cache[resultSig] = this;

            if (!Modal)
            {
                var urlString = new UrlString();
                urlString.Add("sid", resultSig);
                var appItem =
                    Database.GetDatabase("core").GetItem("/sitecore/content/Applications/PowerShell/PowerShellListView");
                Sitecore.Shell.Framework.Windows.RunApplication(appItem, "Business/32x32/table_sql_view.png", Title, urlString.Query);
            }
            else
            {
                var urlString = new UrlString(UIUtil.GetUri("control:PowerShellResultViewerList"));
                urlString.Add("sid", resultSig);
                SheerResponse.ShowModalDialog(urlString.ToString(), Width, Height);
            }
        }
    }
}