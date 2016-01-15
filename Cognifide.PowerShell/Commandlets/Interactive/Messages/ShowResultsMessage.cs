using System;
using System.Web;
using Sitecore;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Commandlets.Interactive.Messages
{
    [Serializable]
    public class ShowResultsMessage : BasePipelineMessage
    {
        public ShowResultsMessage(string html, string width, string height, string foregroundColor, string backgroundColor)
        {
            Html = html;
            Width = width;
            Height = height;
            ForegroundColor = foregroundColor;
            BackgroundColor = backgroundColor;
        }


        public string ForegroundColor { get; set; }
        public string BackgroundColor { get; set; }

        public string Html { get; private set; }
        public string Width { get; private set; }
        public string Height { get; private set; }

        /// <summary>
        ///     Shows a confirmation dialog.
        /// </summary>
        protected override void ShowUI()
        {
            var resultSig = Guid.NewGuid().ToString();
            HttpContext.Current.Session[resultSig] = Html;
            var urlString = new UrlString(UIUtil.GetUri("control:PowerShellResultViewerText"));
            urlString.Add("sid", resultSig);
            if (!string.IsNullOrEmpty(ForegroundColor))
            {
                urlString.Add("fc", ForegroundColor);
            }
            if (!string.IsNullOrEmpty(BackgroundColor))
            {
                urlString.Add("bc", BackgroundColor);
            }
            SheerResponse.ShowModalDialog(urlString.ToString(), Width, Height);
        }
    }
}