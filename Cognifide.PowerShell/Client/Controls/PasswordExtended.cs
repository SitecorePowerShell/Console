using Sitecore.Globalization;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Client.Controls
{
    public class PasswordExtended : Password
    {
        public string PlaceholderText
        {
            get
            {
                return GetViewStateString("Placeholder");
            }
            set
            {
                if (PlaceholderText == value) { return; }

                Attributes["placeholder"] = value;
                SetViewStateString("Placeholder", value);
                SheerResponse.SetAttribute(this.ID, "placeholder", Translate.Text(value));
            }
        }
    }
}