using System;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Resources.Media;
using Sitecore.Shell.Framework;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages
{
    [Serializable]
    public class DownloadMessage : BasePipelineMessage, IMessage
    {
        private Item item;
        private string fileName;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Sitecore.Jobs.AsyncUI.ConfirmMessage"/> class.
        /// 
        /// </summary>
        /// <param name="message">The message.</param>
        public DownloadMessage(Item item)
        {
            this.item = item;
        }

        public DownloadMessage(string fileName)
        {
            this.fileName = fileName;
        }

        /// <summary>
        /// Shows a confirmation dialog.
        /// 
        /// </summary>
        protected override void ShowUI()
        {
            if (item != null && MediaManager.HasMediaContent(item))
            {
                UrlString str = item.Uri.ToUrlString(string.Empty);
                str.Append("field", "Blob");
                Files.Download(str.ToString());
                Log.Audit(this, "Download file: {0}", new string[] {str.ToString()});
            }
            else if (!string.IsNullOrEmpty(fileName))
            {
                SheerResponse.Download(fileName);
            }
            else
            {
                SheerResponse.Alert("There is no file attached.", new string[0]);
            }
        }
    }
}