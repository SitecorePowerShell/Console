using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Core.Settings.Authorization
{
    public static class SessionElevationErrors
    {
        public const string MessageOperationRequiresElevation =
            "Operation cannot be performed due to session elevation restrictions. Elevate your session and try again.";

        public const string MessageOperationFailedWrongDataTemplate =
            "Script cannot be executed as it is of a wrong data template!";

        public static ClientCommand OperationRequiresElevation()
        {
            return SheerResponse.Alert(MessageOperationRequiresElevation);
        }

        public static ClientCommand OperationFailedWrongDataTemplate()
        {
            return SheerResponse.Alert(MessageOperationFailedWrongDataTemplate);
        }

    }
}