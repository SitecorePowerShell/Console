using System;
using System.Web;
using Sitecore;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Spe.Commands.Interactive.Messages
{
    [Serializable]
    public class ShowResultsMessage : BasePipelineMessageWithResult
    {
        public ShowResultsMessage(string payload, string width, string height, string foregroundColor, string backgroundColor,
            string title, string icon, string description)
        {
            Payload = payload;
            Width = width;
            Height = height;
            ForegroundColor = foregroundColor;
            BackgroundColor = backgroundColor;
            Title = title;
            Icon = icon;
            Description = description;
        }

        public string ForegroundColor { get; set; }
        public string BackgroundColor { get; set; }
        // jsterm-format payload the PowerShellResultViewerText terminal echoes.
        // Historical name was "Html"; the viewer moved from rendered HTML to a
        // jquery.terminal instance so it can virtualise long output.
        public string Payload { get; private set; }
        public string Width { get; private set; }
        public string Height { get; private set; }
        public string Title { get; private set; }
        public string Icon { get; private set; }
        public string Description { get; private set; }

        /// <summary>
        ///     Shows a confirmation dialog.
        /// </summary>
        protected override void ShowUI()
        {
            var resultSig = Guid.NewGuid().ToString();
            HttpContext.Current.Session[resultSig] = Payload;
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
            if (!string.IsNullOrEmpty(Title))
            {
                urlString.Add("title", Title);
            }
            if (!string.IsNullOrEmpty(Icon))
            {
                urlString.Add("icon", Icon);
            }
            if (!string.IsNullOrEmpty(Description))
            {
                urlString.Add("desc", Description);
            }
            SheerResponse.ShowModalDialog(urlString.ToString(), Width, Height,"", true);
        }
    }
}