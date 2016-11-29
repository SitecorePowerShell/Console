using System;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Cognifide.PowerShell.Core.VersionDecoupling.Interfaces;
using Sitecore;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Events;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Core.Settings.Authorization
{
    [Serializable]
    public class ScriptItemSecurityEventHandler
    {

        public void OnEvent(object sender, EventArgs args)
        {
            Item item = null;
            var scArgs = args as SitecoreEventArgs;
            if (scArgs == null)
            {
                return;
            }
            item = scArgs.Parameters[0] as Item;
            if (item != null && !item.InheritsFrom(TemplateIDs.ScriptTemplate) && !item.InheritsFrom(TemplateIDs.ScriptLibraryTemplate))
            {
                return;
            }

            if (!SessionElevationManager.IsSessionTokenElevated(SessionElevationManager.ItemSave))
            {
                SheerResponse.Alert(
                    "Operation cannot be performed due to session elevation restrictions. Elevate your session and try again");

                var creatingArgs = scArgs.Parameters[0] as ItemCreatingEventArgs;
                if (creatingArgs != null)
                {
                    creatingArgs.Cancel = true;
                }

                scArgs.Result.Cancel = true;
                scArgs.Result.Messages.Add("Item save prevented");
            }
        }
    }
}