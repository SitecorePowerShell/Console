using System;
using System.Web;
using Sitecore;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Events;
using Sitecore.SecurityModel;
using Spe.Core.Diagnostics;
using Spe.Core.Extensions;

namespace Spe.Core.Settings.Authorization
{
    [Serializable]
    public class ScriptItemSecurityEventHandler
    {
        public void OnEvent(object sender, EventArgs args)
        {
            //Should we adhere to EventDisabler? Are there cases in which the events are disabled
            //through c# code that would matter?

            if (!(args is SitecoreEventArgs scArgs) || HttpContext.Current?.Session == null || scArgs.Parameters.Length < 1 ||
                SecurityDisabler.CurrentValue == SecurityState.Disabled)
            {
                // allow jobs to modify scripts as otherwise all kind of things break
                // allow modifying scripts when SecurityDisabler is active - needed for Update Packages to function
                return;
            }

            var item = scArgs.Parameters[0] as Item;
            if (item != null && !item.IsUnderScriptLibrary() && !item.IsUnderAccessRights())
            {
                // not under a protected path
                return;
            }

            var itemCreatingEventArgs = scArgs.Parameters[0] as ItemCreatingEventArgs;
            if (itemCreatingEventArgs?.Parent != null
                && !itemCreatingEventArgs.Parent.IsUnderScriptLibrary()
                && !itemCreatingEventArgs.Parent.IsUnderAccessRights())
            {
                // not creating under a protected path
                return;
            }

            if (!SessionElevationManager.IsSessionTokenElevated(ApplicationNames.ItemSave))
            {
                SessionElevationErrors.OperationRequiresElevation();

                if (itemCreatingEventArgs != null)
                {
                    itemCreatingEventArgs.Cancel = true;
                    PowerShellLog.Audit(
                        $"[Security] action=creationBlocked path=\"{itemCreatingEventArgs.Parent?.Paths?.Path}/{itemCreatingEventArgs.ItemName}\"");
                }
                else
                {
                    PowerShellLog.Audit(
                        $"[Security] action=saveBlocked path=\"{item?.Parent.Paths.Path}\"");
                }

                scArgs.Result.Cancel = true;
                scArgs.Result.Messages.Add("Item save prevented");
                return;
            }

            if (itemCreatingEventArgs != null)
            {
                PowerShellLog.Audit(
                    $"[Security] action=itemCreated path=\"{itemCreatingEventArgs.Parent?.Paths?.Path}/{itemCreatingEventArgs.ItemName}\"");
            }
            else
            {
                PowerShellLog.Audit(
                    $"[Security] action=itemSaved path=\"{item?.Parent.Paths.Path}\"");
                if (item != null && item.IsPowerShellScript())
                {
                    PowerShellLog.Debug(item[Templates.Script.Fields.ScriptBody]);
                }
            }
        }
    }
}