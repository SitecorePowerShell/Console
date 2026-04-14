using System;
using System.Collections.Specialized;
using System.Linq;
using System.Web.UI;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;

namespace Spe.Client.Controls
{
    public class IseRunspaceModePanel : IseContextPanelBase
    {
        protected override Item Button1 => null;
        protected override Item Button2 => null;
        protected override string Label1 => null;
        protected override string Icon1 => null;
        protected override string Label2 => null;
        protected override string Icon2 => null;

        public override void Render(HtmlTextWriter output, Sitecore.Web.UI.WebControls.Ribbons.Ribbon ribbon, Item button, CommandContext context)
        {
            Assert.ArgumentNotNull(output, "output");
            Assert.ArgumentNotNull(context, "context");

            var isConstrained = string.Equals(
                context.Parameters["currentRunspaceMode"],
                "ConstrainedLanguage",
                StringComparison.OrdinalIgnoreCase);
            var disabled = context.Parameters["ScriptRunning"] == "1" ? " disabled=\"disabled\"" : "";
            var checkedAttr = isConstrained ? " checked=\"checked\"" : "";

            output.Write("<div class=\"iseRibbonContextPanel\">");
            output.Write("<div class=\"scRibbonToolbarSmallButtons scRibbonContextLabels\">");
            output.Write("<div class=\"iseRibbonContextPanelLabel\">Runspace Mode</div>");
            output.Write("</div>");
            output.Write("<div class=\"iseRunspaceModeCheckbox\">");
            output.Write("<label title=\"When checked, scripts run in ConstrainedLanguage mode. Use this to test scripts before approving them for a remoting policy.\">");
            output.Write($"<input type=\"checkbox\"{checkedAttr}{disabled} onchange=\"javascript:return scForm.postEvent(this,event,'ise:setrunspacemode(mode='+(this.checked?'ConstrainedLanguage':'FullLanguage')+')')\" />");
            output.Write(" Constrained");
            output.Write("</label>");
            output.Write("</div>");
            output.Write("</div>");
        }
    }
}
