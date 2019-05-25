using Sitecore.Diagnostics;
using Sitecore.Pipelines.GetContentEditorWarnings;
using Spe.Core.Extensions;

namespace Spe.Core.Settings.Authorization
{
    public class ContentEditorSecurityWarning
    {
        public void Process(GetContentEditorWarningsArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            if (!args.Item.IsPowerShellScript() && !args.Item.IsPowerShellLibrary() && !args.Item.InheritsFrom(Templates.ScriptModule.Id))
            {
                return;
            }

            var action = SessionElevationManager.GetToken(ApplicationNames.ItemSave).Action;

            var warning = new GetContentEditorWarningsArgs.ContentEditorWarning();
            switch (action)
            {
                case SessionElevationManager.TokenDefinition.ElevationAction.Password:
                    if (SessionElevationManager.IsSessionTokenElevated(ApplicationNames.ItemSave))
                    {
                        warning.Title = "You have temporarily enabled script viewing and editing.";
                        warning.Text =
                            "Drop access if you no longer require it. For more information, refer to our <a href=\"https://sitecorepowershell.com/session-state-elevation/\" class=\"scEditorWarningOption\" target=\"_blank\">Documentation.</a>";
                        warning.AddOption("Drop access", "item:dropelevatescriptedit");
                        args.Warnings.Add(warning);
                    }
                    else
                    {
                        warning.HideFields = true;
                        warning.Title = "Elevated session state is required to view and edit scripts.";
                        warning.Text =
                            "A security dialog will prompt you for your credentials before allowing access to view and edit scripts. For more information, refer to our <a href=\"https://sitecorepowershell.com/session-state-elevation/\" class=\"scEditorWarningOption\" target=\"_blank\">Documentation.</a>";
                        warning.AddOption("Elevate session", "item:elevatescriptedit");
                        args.Warnings.Add(warning);
                    }
                    break;
                case SessionElevationManager.TokenDefinition.ElevationAction.Block:
                    warning.HideFields = true;
                    warning.Title = "Elevated session state is blocked. Access to view and edit scripts is disabled.";
                    warning.Text =
                        "For more information, refer to our <a href=\"https://sitecorepowershell.com/session-state-elevation/\" class=\"scEditorWarningOption\" target=\"_blank\">Documentation.</a>";
                    args.Warnings.Add(warning);
                    break;
            }
        }
    }
}