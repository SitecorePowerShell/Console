using System;
using System.Collections;
using System.IO;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.IO;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Resources.Media;
using Sitecore.Shell.Framework;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;
using Version = Sitecore.Data.Version;

namespace Cognifide.PowerShell.Commandlets.Interactive.Messages
{
    [Serializable]
    public class DownloadMessage : BasePipelineMessageWithResult
    {
        public string FileName { get; }
        public string ItemDb { get; }
        public string ItemUri { get; }
        public bool NoDialog { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public bool ShowFullPath { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }

        public DownloadMessage(Item item)
        {
            ItemUri = item.Uri.ToDataUri().ToString(); 
            ItemDb = item.Database.Name;
        }

        public DownloadMessage(string fileName)
        {
            FileName = fileName;
        }

        /// <summary>
        ///     Shows a confirmation dialog.
        /// </summary>
        protected override void ShowUI()
        {
            Item item = null;
            if (ItemDb != null)
            {
                item = Factory.GetDatabase(ItemDb).GetItem(ItemUri);
            }

            if (NoDialog)
            {
                if (item != null && MediaManager.HasMediaContent(item))
                {
                    var str = item.Uri.ToUrlString(string.Empty);
                    str.Append("field", "Blob");
                    Files.Download(str.ToString());
                    PowerShellLog.Audit("Download file: {0}", str.ToString());
                }
                else if (!string.IsNullOrEmpty(FileName))
                {
                    SheerResponse.Download(FileName);
                }
                else
                {
                    SheerResponse.Alert("There is no file attached.");
                }
            }
            else
            {
                var urlString = new UrlString("sitecore/shell/default.aspx?xmlcontrol=DownloadFile");
                var handle = new UrlHandle();
                handle["te"] = Message ?? string.Empty;
                handle["fn"] = FileName ?? string.Empty;
                handle["cp"] = Title ?? string.Empty;
                handle["fp"] = ShowFullPath.ToString();
                handle["uri"] = ItemUri ?? string.Empty;
                handle["db"] = ItemDb ?? string.Empty;
                handle.Add(urlString);

                SheerResponse.ShowModalDialog(
                    urlString.ToString(),
                    Width ?? "600",
                    Height ?? "200",
                    string.Empty,
                    true
                    );
            }
        }

        protected override object ProcessResult(bool hasResult, string result)
        {
            return result;
        }
    }
}