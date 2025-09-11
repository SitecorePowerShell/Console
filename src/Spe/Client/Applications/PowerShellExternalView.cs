using System;
using System.Text.RegularExpressions;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;

namespace Spe.Client.Applications
{
    public class PowerShellExternalView : BaseForm
    {
        public Literal Result;
        public Literal DialogHeader;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            var xssCleanup = new Regex(@"<script[^>]*>[\s\S]*?</script>|<noscript[^>]*>[\s\S]*?</noscript>|<img.*onerror.*>");
            var errorMessage = "Unable to properly parse the query string. It may contain unsafe code.";
            var title = WebUtil.GetQueryString("spe_t");
            if (!string.IsNullOrEmpty(title))
            {
                if (xssCleanup.IsMatch(title))
                {
                    DialogHeader.Text = errorMessage;
                }
                else
                {
                    DialogHeader.Text = title;
                }
            }
            var url = WebUtil.GetQueryString("spe_url");
            if(string.IsNullOrEmpty(url)) 
            {
                url = "/sitecore%20modules/PowerShell/Assets/version.html";
            }

            var urlStr = new UrlString(url);
            var urlParams = WebUtil.ParseQueryString(WebUtil.GetRawUrl());
            foreach (var key in urlParams.Keys)
            {
                if (!key.StartsWith("spe_", StringComparison.OrdinalIgnoreCase) &&
                    !key.Equals("xmlcontrol", StringComparison.OrdinalIgnoreCase))
                {
                    urlStr.Parameters.Add(key, urlParams[key]);
                }
            }

            if(xssCleanup.IsMatch(urlStr.ToString()))
            {
                Result.Text = errorMessage;
            }
            else
            {
                Result.Text = $"<iframe class='externalViewer' src='{urlStr}'></iframe>";
            }
        }

    }
}
