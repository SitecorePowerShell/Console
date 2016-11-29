using System;
using System.Linq;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Modules;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.GetContentEditorWarnings;

namespace Cognifide.PowerShell.Core.Settings.Authorization
{
    public class ContentEditorSecurityWarning
    {
        public void Process(GetContentEditorWarningsArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            if (!args.Item.InheritsFrom(TemplateIDs.ScriptTemplate) && !args.Item.InheritsFrom(TemplateIDs.ScriptLibraryTemplate) && !args.Item.InheritsFrom(TemplateIDs.ScriptModuleTemplate))
            {
                return;
            }

            var action = SessionElevationManager.GetToken(SessionElevationManager.ItemSave).Action;

            var warning = new GetContentEditorWarningsArgs.ContentEditorWarning();
            switch (action)
            {
                case SessionElevationManager.TokenDefinition.ElevationAction.Password:
                    if (SessionElevationManager.IsSessionTokenElevated(SessionElevationManager.ItemSave))
                    {
                        warning.Title = "You have temporarily enabled script viewing and editing.";
                        warning.Text =
                            "Drop access if you no longer require it. For more information, refer to our <a href=\"http://blog.najmanowicz.com/session-state-elevation-concept-in-sitecore-powershell-extensions/\" class=\"scEditorWarningOption\" target=\"_blank\">Documentation</a>";
                        warning.AddOption("Drop access", "item:dropelevatescriptedit");
                        args.Warnings.Add(warning);
                    }
                    else
                    {
                        warning.HideFields = true;
                        warning.Title = "Session privilege elevation is required to save and edit scripts.";
                        warning.Text =
                            "To view, edit or create a script you will be asked for your password to elevate your session privileges. For more information, refer to our <a href=\"http://blog.najmanowicz.com/session-state-elevation-concept-in-sitecore-powershell-extensions/\" class=\"scEditorWarningOption\" target=\"_blank\">Documentation</a>";
                        warning.AddOption("Elevate session", "item:elevatescriptedit");
                        args.Warnings.Add(warning);
                    }
                    break;
                case SessionElevationManager.TokenDefinition.ElevationAction.Block:
                    warning.HideFields = true;
                    warning.Title = "Session privilege elevation is blocked and you cannot save or view scripts.";
                    warning.Text =
                        "For more information, refer to our <a href=\"http://blog.najmanowicz.com/session-state-elevation-concept-in-sitecore-powershell-extensions/\" class=\"scEditorWarningOption\" target=\"_blank\">Documentation</a>";
                    args.Warnings.Add(warning);
                    break;
            }
        }
    }
}