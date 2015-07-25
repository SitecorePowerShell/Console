using Sitecore.Globalization;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Client.Controls
{
    public class EditExtended : Edit
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