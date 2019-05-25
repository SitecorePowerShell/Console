using System;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Resources.Media;
using Sitecore.Shell.Framework;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;
using Spe.Core.Diagnostics;

namespace Spe.Commands.Interactive.Messages
{
    [Serializable]
    public class DownloadMessage : BasePipelineMessageWithResult
    {
        public string FileName { get; }
        public string ItemDb { get; }
        public string ItemUri { get; }
        public string ItemId { get; }
        public bool NoDialog { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public bool ShowFullPath { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }

        public DownloadMessage(Item item)
        {
            ItemId = item.ID.ToString();
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
                item = Factory.GetDatabase(ItemDb).GetItem(ItemId);
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
                    SheerResponse.Alert(Texts.DownloadFile_No_file_attached);
                }
            }
            else
            {
                var urlString = new UrlString("sitecore/shell/default.aspx?xmlcontrol=DownloadFile");
                var handle = new UrlHandle
                {
                    ["te"] = Message ?? string.Empty,
                    ["fn"] = FileName ?? string.Empty,
                    ["cp"] = Title ?? string.Empty,
                    ["fp"] = ShowFullPath.ToString(),
                    ["uri"] = ItemUri ?? string.Empty,
                    ["db"] = ItemDb ?? string.Empty
                };
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