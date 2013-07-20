using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Web.UI.HtmlControls;

namespace Cognifide.PowerShell.SitecoreIntegrations
{
    public class PowerShellUiUserOptions
    {
        public static bool ShowContextMenuScripts
        {
            get
            {
                return Registry.GetBool("/Current_User/UserOptions.ContentEditor.ShowContextMenuScripts", true);
            }
            set
            {
                Registry.SetBool("/Current_User/UserOptions.ContentEditor.ShowContextMenuScripts", value);
            }
        }

        public static bool ShowContextMenuTerminal
        {
            get
            {
                return Registry.GetBool("/Current_User/UserOptions.ContentEditor.ShowContextMenuTerminal", true);
            }
            set
            {
                Registry.SetBool("/Current_User/UserOptions.ContentEditor.ShowContextMenuTerminal", value);
            }
        }
    }
}