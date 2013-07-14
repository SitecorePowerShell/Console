using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Web.UI;
using Cognifide.PowerShell.PowerShellIntegrations;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Templates;
using Sitecore.Diagnostics;
using Sitecore.Form.UI.Controls;
using Sitecore.Install.Utils;
using Sitecore.Shell.Applications.Layouts.IDE.Editors.Xslt;
using Sitecore.Shell.Applications.WebEdit;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.SitecoreIntegrations.Commands
{
    [Serializable]
    public class RebuildControlPanelIntegration : Command
    {

        protected const string FieldParameter = "field";
        protected const string LibraryParameter = "library";
        protected const string LibraryTemplateParameter = "libraryTemplate";
        protected const string ScriptTemplateParameter = "scriptTemplate";
        protected const string scriptLibPath = "/sitecore/system/Modules/PowerShell/Script Library/Control Panel/";
        protected const string controlPanelPath = "/sitecore/content/Applications/Control Panel/";

        protected Item CurrentItem { get; set; }
        protected Item SettingsItem { get; set; }

        public override CommandState QueryState(CommandContext context)
        {
            return CommandState.Enabled;
        }


        /// <summary>
        ///     Executes the command in the specified context.
        /// </summary>
        /// <param name="context">
        ///     The context.
        /// </param>
        public override void Execute(CommandContext context)
        {
            Assert.ArgumentNotNull(context, "context");
            Database coreDb = Factory.GetDatabase("core");
            Database masterDb = Factory.GetDatabase("master");
            TemplateItem sectionTemplate = coreDb.GetTemplate(new ID("{08173C9E-EF72-4385-9634-51411B739E41}"));
            TemplateItem taskTemplate = coreDb.GetTemplate(new ID("{BDB6FA46-2F76-4BDE-8138-52B56C2FC47E}"));

            // create or update script references for existing scripts
            var libRoot = masterDb.GetItem(scriptLibPath);
            if (libRoot != null)
            {
                foreach (Item libSection in libRoot.Children)
                {
                    var sectionName = libSection.Name;
                    var cpSectionPath = controlPanelPath + sectionName;
                    var cpSection = coreDb.GetItem(cpSectionPath);
                    if (cpSection == null)
                    {
                        continue;
                    }
                    var scripts = libSection.Children;
                    foreach (Item script in scripts)
                    {
                        string entryPath = cpSectionPath + "/" + script.Name;
                        Item cpEntry = coreDb.GetItem(entryPath) ??
                                       coreDb.CreateItemPath(entryPath, sectionTemplate, taskTemplate);
                        Item scriptItem = script;
                        string clickFieldValue = cpEntry["Click"];
                        if (string.IsNullOrEmpty(clickFieldValue) ||
                            clickFieldValue.StartsWith("item:executescript", StringComparison.OrdinalIgnoreCase))
                        {
                            cpEntry.Edit(args =>
                            {
                                scriptItem.Fields.ReadAll();
                                cpEntry["__Display name"] = scriptItem.DisplayName;
                                cpEntry["Header"] = scriptItem.DisplayName;
                                cpEntry["__Icon"] = scriptItem["__Icon"];
                                cpEntry["Click"] =
                                    string.Format("item:executescript(id=$Target,script={0},scriptDb={1})",
                                        scriptItem.ID, scriptItem.Database.Name);
                            });
                        }
                    }
                }

                // cleanup after deleted or renamed scripts
                var cpItem = coreDb.GetItem(controlPanelPath);
                foreach (Item cpSection in cpItem.Children)
                {
                    foreach (Item potentialScriptReferenceItem in cpSection.Children)
                    {
                        string clickFieldValue = potentialScriptReferenceItem["Click"];
                        if (clickFieldValue.StartsWith("item:executescript", StringComparison.OrdinalIgnoreCase))
                        {
                            // it's a script, let's check if it exists in library
                            var scriptItem = masterDb.GetItem(scriptLibPath + potentialScriptReferenceItem.Parent.Name + "/" +
                                             potentialScriptReferenceItem.Name);
                            if (scriptItem == null)
                            {
                                potentialScriptReferenceItem.Delete();
                                Log.Info(
                                    String.Format("Removing not existing script reference '{0}'from Control Panel",potentialScriptReferenceItem.Name), 
                                this);
                            }
                        }
                    }
                }
            }
        }
    }
}