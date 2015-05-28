using System;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Resources.Media;
using Sitecore.Shell.Framework;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Commandlets.Interactive.Messages
{
    [Serializable]
    public class DownloadMessage : BasePipelineMessage
    {
        [NonSerialized] private readonly string fileName;
        [NonSerialized] private readonly Item item;

        public DownloadMessage(Item item)
        {
            this.item = item;
        }

        public DownloadMessage(string fileName)
        {
            this.fileName = fileName;
        }

        /// <summary>
        ///     Shows a confirmation dialog.
        /// </summary>
        protected override void ShowUI()
        {
            if (item != null && MediaManager.HasMediaContent(item))
            {
                var str = item.Uri.ToUrlString(string.Empty);
                str.Append("field", "Blob");
                Files.Download(str.ToString());
                Log.Audit(this, "Download file: {0}", str.ToString());
            }
            else if (!string.IsNullOrEmpty(fileName))
            {
                SheerResponse.Download(fileName);
            }
            else
            {
                SheerResponse.Alert("There is no file attached.");
            }
        }
    }
}