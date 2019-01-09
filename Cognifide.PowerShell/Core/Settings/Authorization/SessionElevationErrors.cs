using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Core.Settings.Authorization
{
    public static class SessionElevationErrors
    {
        public static ClientCommand OperationRequiresElevation()
        {
            return SheerResponse.Alert(Texts.PowerShellSessionElevation_Operation_requires_elevation);
        }

        public static ClientCommand OperationFailedWrongDataTemplate()
        {
            return SheerResponse.Alert(Texts.General_Operation_failed_wrong_data_template);
        }

    }
}