using System;
using Sitecore.Data;
using Sitecore.Jobs.AsyncUI;
using Sitecore.StringExtensions;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;

namespace Spe.Commandlets.Interactive.Messages
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
            ContentType= contentType.IsNullOrEmpty()
                            ? "application/octet-stream"
                            : contentType;
            Name = name.IsNullOrEmpty() ? "document.txt" : name;
            Handle = ID.NewID.ToShortID().ToString();
        }

        /// <summary>
        ///     Shows a confirmation dialog.
        /// </summary>
        protected override void ShowUI()
        {
            WebUtil.SetSessionValue(Handle, this);
            SheerResponse.Eval($"spe.DownloadReport('{Handle}');");
        }
    }
}