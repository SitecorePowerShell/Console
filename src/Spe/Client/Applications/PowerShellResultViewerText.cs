using System;
using System.Web;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Spe.Core.Host;
using Spe.Core.Settings;

namespace Spe.Client.Applications
{
    public class PowerShellResultViewerText : BaseForm
    {
        // Result is retained as a hidden Literal so external callers that may
        // still look it up by id don't break. PayloadScript is how we feed the
        // jsterm payload to the jquery.terminal initialiser without tripping
        // on HTML entity decoding that would corrupt jsterm's bracket format
        // markers. All is the outer content border that fills the panel; it
        // gets the session background so there are no white margins around
        // the terminal. TerminalHost is the div the terminal binds to.
        protected Literal Result;
        protected Literal PayloadScript;
        protected Literal DialogHeader;
        protected Literal PsProgressStatus;
        protected ThemedImage Icon;
        protected Border All;
        protected Border TerminalHost;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            var sid = WebUtil.SafeEncode(WebUtil.GetQueryString("sid"));
            var title = WebUtil.GetQueryString("title");
            var icon = WebUtil.GetQueryString("icon");
            if (!string.IsNullOrEmpty(title))
            {
                DialogHeader.Text = title;
            }
            if (!string.IsNullOrEmpty(icon))
            {
                Icon.Src = icon;
            }
            var description = WebUtil.GetQueryString("desc");
            if (!string.IsNullOrEmpty(description))
            {
                PsProgressStatus.Text = description;
            }

            // Match the session's foreground/background onto the terminal host
            // so Show-Result -Text and the Runner's "View Script Results" link
            // both honour the user's session colour settings instead of the
            // jquery.terminal default black/white.
            var settings = ApplicationSettings.GetInstance(ApplicationNames.Context, false);
            if (!Enum.TryParse(WebUtil.GetQueryString("fc"), true, out ConsoleColor fc))
            {
                fc = settings.ForegroundColor;
            }
            if (!Enum.TryParse(WebUtil.GetQueryString("bc"), true, out ConsoleColor bc))
            {
                bc = settings.BackgroundColor;
            }
            var foregroundColor = OutputLine.ProcessHtmlColor(fc);
            var backgroundColor = OutputLine.ProcessHtmlColor(bc);
            // Paint the session colours all the way out to the scrollbox wrapper
            // so there are no white margins around the terminal - the terminal
            // itself then inherits the same colours via #resultTerminal CSS.
            if (All != null)
            {
                All.Style.Add("color", foregroundColor);
                All.Style.Add("background-color", backgroundColor);
                All.Style.Add("font-family", settings.FontFamilyStyle);
                All.Style.Add("font-size", $"{settings.FontSize}px");
            }
            if (TerminalHost != null)
            {
                TerminalHost.Style.Add("color", foregroundColor);
                TerminalHost.Style.Add("background-color", backgroundColor);
                TerminalHost.Style.Add("font-family", settings.FontFamilyStyle);
                TerminalHost.Style.Add("font-size", $"{settings.FontSize}px");
            }

            var payload = HttpContext.Current.Session[sid] as string ?? string.Empty;
            HttpContext.Current.Session.Remove(sid);

            // Passing the jsterm string through a JavaScriptStringEncode'd
            // variable avoids HTML-entity round-trips (&#91; -> [ -> broken
            // jsterm parsing). speResultViewerInit is declared earlier in the
            // page; calling it right after assigning the payload guarantees
            // the terminal sees the data regardless of where Sitecore's
            // FormPage ends up placing any given inline <Script> block.
            var encoded = HttpUtility.JavaScriptStringEncode(payload);
            PayloadScript.Text =
                $"<script>window.resultPayload = \"{encoded}\"; " +
                $"if (window.speResultViewerInit) window.speResultViewerInit();</script>";

            SheerResponse.SetDialogValue(sid);
        }
    }
}
