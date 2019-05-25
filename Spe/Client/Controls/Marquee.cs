using System.Web.UI;
using Sitecore.Diagnostics;
using Sitecore.Web.UI.Sheer;
using Control = Sitecore.Web.UI.HtmlControls.Control;

namespace Spe.Client.Controls
{
    public class Marquee : Control
    {
        private string _innerHtml;

        public string InnerHtml
        {
            get
            {
                return this._innerHtml;
            }
            set
            {
                Error.AssertString(value, nameof (InnerHtml), true);
                this._innerHtml = value;
                SheerResponse.SetInnerHtml(this.ID, value);
            }
        }

        protected override void DoRender(HtmlTextWriter output)
        {
            output.Write("<marquee" + this.ControlAttributes + ">");
            if (this.InnerHtml != null)
            {
                output.Write(this.InnerHtml);
            }

            this.RenderChildren(output);
            output.Write("</marquee>");
        }
    }
}