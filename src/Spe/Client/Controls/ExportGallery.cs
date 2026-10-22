using System;
using Sitecore;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Resources;
using Sitecore.Shell.Applications.ContentManager.Galleries;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.XmlControls;
using Spe.Core.Extensions;

namespace Spe.Client.Controls
{
    // Dropdown of the ListView export split button. The ribbon panel has already applied
    // the exporters' show/enable rules and passes the result in the query string.
    public class ExportGallery : GalleryForm
    {
        protected GalleryMenu Options;
        protected Scrollbox Exporters;

        public override void HandleMessage(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            if (message.Name != "event:click")
            {
                Invoke(message, true);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (Context.ClientPage.IsEvent)
            {
                return;
            }

            var current = WebUtil.GetQueryString("current");
            var disabled = new ListString(WebUtil.GetQueryString("disabled"));
            foreach (var uri in new ListString(WebUtil.GetQueryString("exporters")))
            {
                var scriptItem = Database.GetItem(ItemUri.Parse(uri));
                if (!scriptItem.IsPowerShellScript())
                {
                    continue;
                }

                var control = ControlFactory.GetControl("ExportGallery.Option") as XmlControl;
                Assert.IsNotNull(control, "Xml Control \"ExportGallery.Option\" not found");
                Context.ClientPage.AddControl(Exporters, control);

                var builder = new ImageBuilder
                {
                    Src = Images.GetThemedImageSource(scriptItem[FieldIDs.Icon], ImageDimension.id16x16),
                    Class = "scRibbonToolbarSmallGalleryButtonIcon",
                    Alt = scriptItem.DisplayName
                };
                var isDisabled = disabled.Contains(uri);
                control["Class"] = isDisabled ? "disabled" : scriptItem.ID.ToString() == current ? "selected" : string.Empty;
                control["ExporterIcon"] = $"<div class=\"versionNum\">{builder}</div>";
                control["Name"] = Translate.Text(scriptItem.DisplayName);
                control["ToolTip"] = scriptItem.Appearance.ShortDescription;
                control["Click"] = isDisabled
                    ? string.Empty
                    : $"export:results(scriptDb={scriptItem.Database.Name},scriptID={scriptItem.ID})";
            }
        }
    }
}
