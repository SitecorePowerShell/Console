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
using Sitecore.Jobs;
using Sitecore.Shell.Applications.WebEdit;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.SitecoreIntegrations.Applications
{
    [Serializable]
    public class FieldEditor : Command
    {
        private const string UriParameter = "uri";
        private const string PathParameter = "path";
        private const string PreserveSectionsParameter = "preservesections";
        private const string CurrentItemIsNull = "Current item is null";

        protected Item CurrentItem { get; set; }

        protected virtual PageEditFieldEditorOptions GetOptions(ClientPipelineArgs args, NameValueCollection form)
        {
            EnsureContext(args);
            var options = new PageEditFieldEditorOptions(form, BuildListWithFieldsToShow(args.Parameters["fields"]))
            {
                Title = args.Parameters["section"],
                Icon = args.Parameters["icon"]
            };
            options.Parameters["contentitem"] = CurrentItem.Uri.ToString();
            options.PreserveSections = args.Parameters[PreserveSectionsParameter] == "1";
            options.DialogTitle = args.Parameters["title"];
            options.SaveItem = true;
            return options;
        }

        protected virtual void EnsureContext(ClientPipelineArgs args)
        {
            string path = args.Parameters[PathParameter];
            Item currentItem = Database.GetItem(ItemUri.Parse(args.Parameters[UriParameter]));

            CurrentItem = string.IsNullOrEmpty(path) ? currentItem : Client.ContentDatabase.GetItem(path);
            Assert.IsNotNull(CurrentItem, CurrentItemIsNull);
        }

        private IEnumerable<FieldDescriptor> BuildListWithFieldsToShow(string fieldString)
        {
            var fieldList = new List<FieldDescriptor>();

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
                    Field field = CurrentItem.Fields[fieldName.Substring(1, fieldName.Length - 1)];
                    if (field != null)
                    {
                        ID fieldId = field.ID;
                        foreach (var fieldDescriptor in fieldList)
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

                if (CurrentItem.Fields[fieldName] != null)
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
            HttpContext current = HttpContext.Current;
            if (current == null)
                return;
            var page = current.Handler as Page;
            if (page == null)
                return;
            NameValueCollection form = page.Request.Form;

            if (!args.IsPostBack)
            {
                SheerResponse.ShowModalDialog(
                    GetOptions(args, form).ToUrlString().ToString(), 
                    args.Parameters["width"],
                    args.Parameters["height"],
                    string.Empty, true);
                args.WaitForPostBack();
            }
            else
            {                   
                if (args.HasResult)
                {
                    PageEditFieldEditorOptions results = PageEditFieldEditorOptions.Parse(args.Result);

                    CurrentItem.Edit(options =>
                    {
                        foreach (var field in results.Fields)
                        {
                            CurrentItem.Fields[field.FieldID].Value = field.Value;
                        }
                    });

                    PageEditFieldEditorOptions.Parse(args.Result).SetPageEditorFieldValues();
                }
                var strJobId = args.Parameters["jobHandle"];
                if (!String.IsNullOrEmpty(strJobId))
                {
                    var jobHandle = Handle.Parse(strJobId);
                    Job job = JobManager.GetJob(jobHandle);
                    if (job != null)
                    {
                        job.MessageQueue.PutResult(args.HasResult ? "ok" : "cancel");
                    }
                }
                else
                {
                    //todo: implement out of job execution
                    //MessageQueue.PutResult(Result);
                }

            }
        }
    }
}