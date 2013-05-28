using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using System.Web.UI;
using Cognifide.PowerShell.PowerShellIntegrations;
using Sitecore;
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
    public class ExecuteFieldEditor : Command
    {
        protected const string FieldName = "Fields";
        protected const string Header = "Header";
        protected const string Icon = "Icon";

        protected const string UriParameter = "uri";
        protected const string ButtonParameter = "button";
        protected const string PathParameter = "path";
        protected const string SaveChangesParameter = "savechanges";
        protected const string ReloadAfterParameter = "reloadafter";
        protected const string PreserveSectionsParameter = "preservesections";
        protected const string CurrentItemIsNull = "Current item is null";
        protected const string SettingsItemIsNull = "Settings item is null";
        protected const string RequireTemplateParameter = "requiretemplate";

        private Item CurrentItem { get; set; }
        private Item SettingsItem { get; set; }

        public override CommandState QueryState(CommandContext context)
        {
            string requiredTemplate = context.Parameters[RequireTemplateParameter];

            if (!string.IsNullOrEmpty(requiredTemplate))
            {
                if (context.Items.Length != 1)
                {
                    return CommandState.Disabled;
                }

                Template template = TemplateManager.GetTemplate(context.Items[0]);
                CommandState result = template.InheritsFrom(requiredTemplate)
                                          ? base.QueryState(context)
                                          : CommandState.Disabled;
                return result;
            }

            return context.Items.Length != 1 || context.Parameters["ScriptRunning"] == "1"
                       ? CommandState.Disabled
                       : CommandState.Enabled;
        }


        protected virtual PageEditFieldEditorOptions GetOptions(ClientPipelineArgs args, NameValueCollection form)
        {
            EnsureContext(args);
            var options = new PageEditFieldEditorOptions(form, BuildListWithFieldsToShow())
                {
                    Title = SettingsItem[Header],
                    Icon = SettingsItem[Icon]
                };
            options.Parameters["contentitem"] = CurrentItem.Uri.ToString();
            options.PreserveSections = args.Parameters[PreserveSectionsParameter] == "1";
            options.DialogTitle = SettingsItem[Header];
            options.SaveItem = true;
            return options;
        }

        private void EnsureContext(ClientPipelineArgs args)
        {
            string path = args.Parameters[PathParameter];
            Item currentItem = Database.GetItem(ItemUri.Parse(args.Parameters[UriParameter]));

            CurrentItem = string.IsNullOrEmpty(path) ? currentItem : Client.ContentDatabase.GetItem(path);
            Assert.IsNotNull(CurrentItem, CurrentItemIsNull);
            SettingsItem = Client.CoreDatabase.GetItem(args.Parameters[ButtonParameter]);
            Assert.IsNotNull(SettingsItem, SettingsItemIsNull);
        }

        private IEnumerable<FieldDescriptor> BuildListWithFieldsToShow()
        {
            var fieldList = new List<FieldDescriptor>();
            var fieldString = new ListString(SettingsItem[FieldName]);

            foreach (string fieldName in new ListString(fieldString))
            {
                // add all non "standard fields"
                if (fieldName == "*")
                {
                    GetNonStandardFields(fieldList);
                    continue;
                }

                // remove fields that are prefixed with a "-" sign
                if (fieldName.IndexOf('-') == 0)
                {
                    Field field = CurrentItem.Fields[fieldName.Substring(1, fieldName.Length - 1)];
                    if (field != null)
                    {
                        ID fieldId = field.ID;
                        foreach (FieldDescriptor fieldDescriptor in fieldList)
                        {
                            if (fieldDescriptor.FieldID == fieldId)
                            {
                                fieldList.Remove(fieldDescriptor);
                                break;
                            }
                        }
                    }
                    continue;
                }

                if (CurrentItem.Fields[fieldName].Definition != null)
                {
                    fieldList.Add(new FieldDescriptor(CurrentItem, fieldName));
                }
            }
            return fieldList;
        }

        private void GetNonStandardFields(List<FieldDescriptor> fieldList)
        {
            CurrentItem.Fields.ReadAll();
            foreach (Field field in CurrentItem.Fields)
            {
                if (field.GetTemplateField().Template.BaseIDs.Length > 0)
                {
                    fieldList.Add(new FieldDescriptor(CurrentItem, field.Name));
                }
            }
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
            if (context.Items.Length < 1)
                return;
            Context.ClientPage.Start(this, "StartFieldEditor", new ClientPipelineArgs(context.Parameters)
                {
                    Parameters =
                        {
                            {"uri", context.Items[0].Uri.ToString()} //,
                            //{"ParentFrameName", context.Parameters["ParentFrameName"]}
                        }
                });
        }

        /// <summary>
        ///     Sheer UI processor methods that orchestrates starting the Field Editor and processing the returned value
        /// </summary>
        /// <param name="args">
        ///     The arguments.
        /// </param>
        protected void StartFieldEditor(ClientPipelineArgs args)
        {
            HttpContext current = HttpContext.Current;
            if (current == null)
                return;
            var page = current.Handler as Page;
            if (page == null)
                return;
            NameValueCollection form = page.Request.Form;
            
            if (!args.IsPostBack)
            {
                SheerResponse.ShowModalDialog(GetOptions(args, form).ToUrlString().ToString(), "720", "520",
                                              string.Empty, true);
                args.WaitForPostBack();
            }
            else
            {
                if (!args.HasResult)
                    return;

                PageEditFieldEditorOptions results = PageEditFieldEditorOptions.Parse(args.Result);

                CurrentItem.Edit(options =>
                    {
                        foreach (FieldDescriptor field in results.Fields)
                        {
                            CurrentItem.Fields[field.FieldID].Value = field.Value;
                        }
                    });

/*
                Context.ClientPage.ServerProperties["ItemID"] = CurrentItem.ID.ToString();
                if (!string.IsNullOrEmpty(args.Parameters["ParentFrameName"]))
                {
                    ScriptInvokationBuilder invokationBuilder = new ScriptInvokationBuilder("scForm", "postMessage");
                    invokationBuilder.AddString("item:updated(id={0})", new object[1]
                                                                                {
                                                                                    CurrentItem.ID.ToString()
                                                                                });
                    invokationBuilder.AddString(args.Parameters["ParentFrameName"], new object[0]);
                    invokationBuilder.AddString("Shell", new object[0]);
                    invokationBuilder.Add(false);
                    SheerResponse.Eval(invokationBuilder.ToString());
                }
*/

                PageEditFieldEditorOptions.Parse(args.Result).SetPageEditorFieldValues();
            }
        }
    }
}