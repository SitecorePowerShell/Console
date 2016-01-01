using System;
using System.IO;
using System.Linq;
using Sitecore;
using Sitecore.Data;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Commandlets.Interactive.Messages
{
    [Serializable]
    public class OutDownloadMessage : BasePipelineMessage
    {
        public object Content { get; }
        public string ContentType { get; }
        public string Name { get; }
        public string Handle { get; }

        public OutDownloadMessage(object content, string name, string contentType)
        {
            Content = content;
            ContentType= string.IsNullOrEmpty(contentType)
                            ? "application/octet-stream"
                            : contentType;
            Name = string.IsNullOrEmpty(name) ? "document.txt" : name;
            Handle = ID.NewID.ToShortID().ToString();
        }

        /// <summary>
        ///     Shows a confirmation dialog.
        /// </summary>
        protected override void ShowUI()
        {
            WebUtil.SetSessionValue(Handle, this);
            SheerResponse.Eval($"cognifide.powershell.DownloadReport('{Handle}');");
        }
    }
}