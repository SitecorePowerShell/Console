using System;
using System.Web;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
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
            if (item != null && !item.IsPowerShellScript() && !item.IsPowerShellLibrary())
            {
                // not a PowerShell related item
                return;
            }

            var itemCreatingEventArgs = scArgs.Parameters[0] as ItemCreatingEventArgs;
            if (itemCreatingEventArgs?.Parent?.Database != null && itemCreatingEventArgs.TemplateId != (ID)null)
            {
                var template = TemplateManager.GetTemplate(itemCreatingEventArgs.TemplateId,
                    itemCreatingEventArgs.Parent.Database);
                if (template == null || (!template.InheritsFrom(Templates.Script.Id) &&
                                         !template.InheritsFrom(Templates.ScriptLibrary.Id)))
                {
                    // not creating Script or Library
                    return;
                }
            }


            if (!SessionElevationManager.IsSessionTokenElevated(ApplicationNames.ItemSave))
            {
                SessionElevationErrors.OperationRequiresElevation();

                if (itemCreatingEventArgs != null)
                {
                    itemCreatingEventArgs.Cancel = true;
                    PowerShellLog.Warn(
                        $"Prevented Script/Library '{itemCreatingEventArgs.Parent?.Paths?.Path}/{itemCreatingEventArgs.ItemName}' creation by '{Context.User?.Name}'.");
                }
                else
                {
                    PowerShellLog.Warn(
                        $"Prevented Script/Library save '{item?.Parent.Paths.Path}' by user '{Context.User?.Name}'.");
                }

                scArgs.Result.Cancel = true;
                scArgs.Result.Messages.Add("Item save prevented");
                return;
            }

            if (itemCreatingEventArgs != null)
            {
                PowerShellLog.Info(
                    $" Script/Library '{itemCreatingEventArgs.Parent?.Paths?.Path}/{itemCreatingEventArgs.ItemName}' created by user '{Context.User?.Name}'");
            }
            else
            {
                PowerShellLog.Info(
                    $" Script/Library saved '{item?.Parent.Paths.Path}' by user '{Context.User?.Name}'");
                if (item.IsPowerShellScript())
                {
                    PowerShellLog.Debug(item[Templates.Script.Fields.ScriptBody]);
                }
            }
        }
    }
}