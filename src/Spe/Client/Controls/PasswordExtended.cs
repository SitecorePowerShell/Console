using Sitecore.Globalization;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Web.UI.Sheer;

namespace Spe.Client.Controls
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