using System;
using Sitecore;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Commandlets.Interactive.Messages
{
    [Serializable]
    public class OutDownloadMessage : BasePipelineMessage
    {
        private readonly object content;
        private readonly string contentType;
        private readonly string name;

        public OutDownloadMessage(object content, string name, string contentType)
        {
            this.content = content;
            this.contentType= contentType;
            this.name = name;
        }

        /// <summary>
        ///     Shows a confirmation dialog.
        /// </summary>
        protected override void ShowUI()
        {
            var handle = new UrlHandle
            {
                ["obj"] = content.ToString(),
                ["ct"] = string.IsNullOrEmpty(contentType)
                            ? "application/octet-stream"
                            : contentType,
                ["name"] = string.IsNullOrEmpty(name) ? "document.txt" : name
            };

            handle.Add(new UrlString("/-/script/handle/"));

            SheerResponse.Eval($"cognifide.powershell.DownloadReport('{ handle.ToHandleString()}');");
        }
    }
}