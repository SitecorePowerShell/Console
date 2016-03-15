using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Management.Automation;
using System.Web;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Sitecore.Shell.Applications.WebEdit;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using Page = Sitecore.Web.UI.HtmlControls.Page;

namespace Cognifide.PowerShell.VersionSpecific.Client.Applications
{
    [Serializable]
    public class FieldEditor : Command
    {
        private const string UriParameter = "uri";
        private const string PathParameter = "path";
        private const string PreserveSectionsParameter = "preservesections";
        private const string IncludeStandardFieldsParameter = "isf";
        private const string CurrentItemIsNull = "Current item is null";
        [NonSerialized] private List<WildcardPattern> excludePatterns;
        [NonSerialized] private List<WildcardPattern> includePatterns;

        protected List<WildcardPattern> IncludePatterns
        {
            get { return includePatterns; }
            private set { includePatterns = value; }
        }

        protected List<WildcardPattern> ExcludePatterns
        {
            get { return excludePatterns; }
            private set { excludePatterns = value; }
        }

        protected bool IncludeStandardFields { get; set; }
        protected ItemUri CurrentItemUri { get; set; }

        protected Item CurrentItem
        {
            get { return Database.GetItem(CurrentItemUri); }
            set { CurrentItemUri = value.Uri; }
        }

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
            var path = args.Parameters[PathParameter];
            var currentItem = Database.GetItem(ItemUri.Parse(args.Parameters[UriParameter]));
            currentItem = string.IsNullOrEmpty(path) ? currentItem : global::Sitecore.Client.ContentDatabase.GetItem(path);
            Assert.IsNotNull(currentItem, CurrentItemIsNull);
            CurrentItem = currentItem;
            IncludeStandardFields = args.Parameters[IncludeStandardFieldsParameter] == "1";
        }

        private IEnumerable<FieldDescriptor> BuildListWithFieldsToShow(string fieldString)
        {
            var fieldList = new List<FieldDescriptor>();
            var fieldNames = new ListString(fieldString).ToArray();

            if (fieldNames.Any())
            {
                IncludePatterns =
                    fieldNames
                        .Where(name => !name.StartsWith("-"))
                        .Select(
                            name =>
                                new WildcardPattern(name, WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant))
                        .ToList();
                ExcludePatterns =
                    fieldNames
                        .Where(name => name.StartsWith("-"))
                        .Select(
                            name =>
                                new WildcardPattern(name.Substring(1),
                                    WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant))
                        .ToList();
            }
            var currentItem = CurrentItem;
            currentItem.Fields.ReadAll();
            var template =
                TemplateManager.GetTemplate(Settings.DefaultBaseTemplate,
                    currentItem.Database);

            FieldCollection fields = new FieldCollection(CurrentItem);
            fields.ReadAll();
            fields.Sort();

            foreach (Field field in fields)
            {
                //if not including standard field and it's standard, skip it.
                if (!IncludeStandardFields && template.ContainsField(field.ID))
                {
                    continue;
                }

                var name = field.Name;
                var wildcardMatch = IncludePatterns.Any(pattern => pattern.IsMatch(name));
                if (!wildcardMatch)
                {
                    continue;
                }
                if (ExcludePatterns.Any(pattern => pattern.IsMatch(name)))
                {
                    wildcardMatch = false;
                }
                if (wildcardMatch)
                {
                    fieldList.Add(new FieldDescriptor(currentItem, field.Name));
                }
            }
            return fieldList;
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
                var strJobId = args.Parameters["jobHandle"];
                if (!String.IsNullOrEmpty(strJobId))
                {
                    var jobHandle = Handle.Parse(strJobId);
                    var job = JobManager.GetJob(jobHandle);
                    if (job != null)
                    {
                        job.MessageQueue.PutResult(args.HasResult ? "ok" : "cancel");
                    }
                }
            }
        }
    }
}