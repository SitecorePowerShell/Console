using System;
using System.Web.UI;

namespace Cognifide.PowerShell.Console.Layouts
{
    public partial class PowerShellTerminal : Page
    {
        protected override void OnPreInit(EventArgs e)
        {
            Response.AddHeader("X-UA-Compatible", "IE=9");
            base.OnPreInit(e);
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }
    }
}