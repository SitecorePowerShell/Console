using System;
using System.Collections.Specialized;
using System.Linq;
using System.Web.UI;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Shell.Applications.ContentEditor.Galleries;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Shell.Web.UI.WebControls;
using Sitecore.Text;
using Sitecore.Web.UI.WebControls.Ribbons;

namespace Cognifide.PowerShell.Client.Controls
{
    public class IseContextPanel : IseContextPanelBase
    {
        protected override Item Button1 => Factory.GetDatabase("core").GetItem("{C733DE04-FFA2-4DCB-8D18-18EB1CB898A3}");
        protected override Item Button2 => Factory.GetDatabase("core").GetItem("{0C784F54-2B46-4EE2-B0BA-72384125E123}");
        protected override string Label1 => ContextItem != null ? ContextItem.GetProviderPath().EllipsisString(50) : Texts.IseContextPanel_Render_none;
        protected override string Icon1 => ContextItem != null ? ContextItem.Appearance.Icon : Button1.Appearance.Icon;
        protected override string Label2 => CommandContext.Parameters["currentSessionName"];
        protected override string Icon2 => string.Empty;

    }
}