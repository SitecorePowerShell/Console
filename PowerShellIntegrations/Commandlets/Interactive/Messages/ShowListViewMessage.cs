using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Jobs.AsyncUI;
using Sitecore.SecurityModel;
using Sitecore.Shell.Framework;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages
{
    public class ShowListViewMessage : IMessage
    {
        public List<ShowListViewCommand.SvlDataObject> Data { get; set; }
        public string Title { get; set; }
        public string Width { get; private set; }
        public string Height { get; private set; }
        public string Icon { get; private set; }
        public int PageSize { get; private set; }
        public ScriptSession Session { get; private set; }
        public bool Modal { get; set; }
        public string InfoTitle { get; private set; }
        public string InfoDescription { get; private set; }
        public object[] Property { get; private set; }


        public ShowListViewMessage(List<ShowListViewCommand.SvlDataObject> data, int pageSize, string title, string icon,
            string width, string height, bool modal, string infoTitle, string infoDescription, object[] property)
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
            Property = property;
        }

        public void Execute()
        {
            string resultSig = Guid.NewGuid().ToString();
            HttpContext.Current.Session[resultSig] = this;
            if (!Modal)
            {
                UrlString urlString = new UrlString();
                urlString.Add("sid", resultSig);
                Item appItem =
                    Database.GetDatabase("core").GetItem("/sitecore/content/Applications/PowerShell/PowerShellListView");
                Windows.RunApplication(appItem, "Business/32x32/table_sql_view.png", Title, urlString.Query);
            }
            else
            {
                UrlString urlString = new UrlString(UIUtil.GetUri("control:PowerShellResultViewerList"));
                urlString.Add("sid", resultSig);
                SheerResponse.ShowModalDialog(urlString.ToString(), Width, Height);
            }
        }
    }
}