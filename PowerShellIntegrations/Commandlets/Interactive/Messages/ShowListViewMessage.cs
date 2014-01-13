using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Framework;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages
{
    public class ShowListViewMessage : IMessage
    {
        public List<BaseListViewCommand.DataObject> Data { get; set; }
        public string Title { get; set; }
        public string Width { get; private set; }
        public string Height { get; private set; }
        public string Icon { get; private set; }
        public int PageSize { get; private set; }
        public ScriptSession Session { get; private set; }
        public bool Modal { get; set; }
        public string InfoTitle { get; private set; }
        public string InfoDescription { get; private set; }
        public Hashtable[] Property { get; private set; }

        public string FormatProperty
        {
            get
            {
                return Property
                    .Select(p => "@{Label=\"" + p["Label"] + "\";Expression={" + p["Expression"] + "}},")
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
                    .Select(p => "@{Label=\"" + p["Label"] + "\";Expression={" + p["Expression"] + "}},")
                    .Aggregate((a, b) => a + b)
                    .TrimEnd(',');
            }
        }

        public string SessionId { get; private set; }
        public object ActionData { get; private set; }

        public ShowListViewMessage(List<ShowListViewCommand.DataObject> data, int pageSize, string title, string icon,
            string width, string height, bool modal, string infoTitle, string infoDescription, string sessionId, object actionData, object[] property)
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

            Property = property
                .Select(p =>
                {
                    var label = string.Empty;
                    var expression = string.Empty;
                    if (p is Hashtable)
                    {
                        Hashtable h = p as Hashtable;
                        if (h.ContainsKey("Name"))
                        {
                            if (!h.ContainsKey("Label"))
                            {
                                h.Add("Label", h["Name"]);
                            }
                        }
                        label = h["Label"].ToString();
                        expression = h["Expression"].ToString();
                    }
                    else
                    {
                        label = p.ToString();
                        expression = "$_."+p.ToString();
                    }
                    var result = new Hashtable(2);
                    result.Add("Label",label);
                    result.Add("Expression",expression);
                    return result;
                }).ToArray();
        }

        public void Execute()
        {
            string resultSig = Guid.NewGuid().ToString();
            HttpContext.Current.Session[resultSig] = this;

            if (!Modal)
            {
                var urlString = new UrlString();
                urlString.Add("sid", resultSig);
                Item appItem =
                    Database.GetDatabase("core").GetItem("/sitecore/content/Applications/PowerShell/PowerShellListView");
                Windows.RunApplication(appItem, "Business/32x32/table_sql_view.png", Title, urlString.Query);
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