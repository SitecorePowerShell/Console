using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Shell.Applications.WebEdit;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Sitecore7.Client.Commands
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
        protected ItemUri CurrentItemUri { get; set; }
        protected ItemUri SettingsItemUri { get; set; }

        protected Item CurrentItem
        {
            get { return Database.GetItem(CurrentItemUri); }
            set { CurrentItemUri = value.Uri; }
        }

        protected Item SettingsItem
        {
            get { return Database.GetItem(SettingsItemUri); }
            set { SettingsItemUri = value.Uri; }
        }

        public override CommandState QueryState(CommandContext context)
        {
            var requiredTemplate = context.Parameters[RequireTemplateParameter];

            if (!string.IsNullOrEmpty(requiredTemplate))
            {
                if (context.Items.Length != 1)
                {
                    return CommandState.Disabled;
                }

                var template = TemplateManager.GetTemplate(context.Items[0]);
                var result = template.InheritsFrom(requiredTemplate)
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
            var settingsItem = SettingsItem;
            var options = new PageEditFieldEditorOptions(form, BuildListWithFieldsToShow())
            {
                Title = settingsItem[Header],
                Icon = settingsItem[Icon]
            };
            options.Parameters["contentitem"] = CurrentItemUri.ToString();
            options.PreserveSections = args.Parameters[PreserveSectionsParameter] == "1";
            options.DialogTitle = settingsItem[Header];
            options.SaveItem = true;
            return options;
        }

        protected virtual void EnsureContext(ClientPipelineArgs args)
        {
            var path = args.Parameters[PathParameter];
            var currentItem = Database.GetItem(ItemUri.Parse(args.Parameters[UriParameter]));

            currentItem = string.IsNullOrEmpty(path) ? currentItem : Sitecore.Client.ContentDatabase.GetItem(path);
            Assert.IsNotNull(currentItem, CurrentItemIsNull);
            CurrentItem = currentItem;
            var settingsItem = Sitecore.Client.CoreDatabase.GetItem(args.Parameters[ButtonParameter]);
            Assert.IsNotNull(settingsItem, SettingsItemIsNull);
            SettingsItem = settingsItem;
        }

        private IEnumerable<FieldDescriptor> BuildListWithFieldsToShow()
        {
            var fieldList = new List<FieldDescriptor>();
            var fieldString = new ListString(SettingsItem[FieldName]);
            var currentItem = CurrentItem;
            foreach (var fieldName in new ListString(fieldString))
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
                    var field = currentItem.Fields[fieldName.Substring(1, fieldName.Length - 1)];
                    if (field != null)
                    {
                        var fieldId = field.ID;
                        foreach (var fieldDescriptor in fieldList.Where(fieldDescriptor => fieldDescriptor.FieldID == fieldId))
                        {
                            fieldList.Remove(fieldDescriptor);
                            break;
                        }
                    }
                    continue;
                }

                if (currentItem.Fields[fieldName] != null)
                {
                    fieldList.Add(new FieldDescriptor(currentItem, fieldName));
                }
            }
            return fieldList;
        }

        private void GetNonStandardFields(ICollection<FieldDescriptor> fieldList)
        {
            var currentItem = CurrentItem;
            currentItem.Fields.ReadAll();
            foreach (Field field in currentItem.Fields)
            {
                if (field.GetTemplateField().Template.BaseIDs.Length > 0)
                {
                    fieldList.Add(new FieldDescriptor(currentItem, field.Name));
                }
            }
        }

        public virtual bool CanExecute(CommandContext context)
        {
            return context.Items.Length > 0;
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
            if (!CanExecute(context))
                return;
            Context.ClientPage.Start(this, "StartFieldEditor", new ClientPipelineArgs(context.Parameters)
            {
                Parameters =
                {
                    {"uri", context.Items[0].Uri.ToString()}
                }
            });
        }

        /// <summary>
        ///     Sheer UI processor methods that orchestrates starting the Field Editor and processing the returned value
        /// </summary>
        /// <param name="args">
        ///     The arguments.
        /// </param>
        protected virtual void StartFieldEditor(ClientPipelineArgs args)
        {
            var current = HttpContext.Current;
            if (current == null)
                return;
            var page = current.Handler as Page;
            if (page == null)
                return;
            var form = page.Request.Form;

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

                var results = PageEditFieldEditorOptions.Parse(args.Result);
                var currentItem = CurrentItem;
                currentItem.Edit(options =>
                {
                    foreach (var field in results.Fields)
                    {
                        currentItem.Fields[field.FieldID].Value = field.Value;
                    }
                });

                PageEditFieldEditorOptions.Parse(args.Result).SetPageEditorFieldValues();
            }
        }
    }
}