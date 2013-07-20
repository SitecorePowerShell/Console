using Sitecore;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.SitecoreIntegrations.Commands
{
    public abstract class ToggleContentEditorIntegrationBase : Command
    {
        protected void RefreshRibbon(CommandContext context, string checkBoxName, bool newValue)
        {
            string executionContext = context.Parameters["context"];
            if (executionContext == "ce")
            {
                SheerResponse.Eval(
                    string.Format("$sc('#Check_{0}').prop('checked', {1});",
                        checkBoxName,
                        newValue.ToString().ToLowerInvariant()));
            }
            else
            {
                Context.ClientPage.SendMessage(this, "ise:updateribbon");
            }
        }
    }
}