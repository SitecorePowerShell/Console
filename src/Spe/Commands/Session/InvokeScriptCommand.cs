using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Spe.Commands.Interactive;
using Spe.Core.Settings;
using Spe.Core.Utility;

namespace Spe.Commands.Session
{
    [Cmdlet(VerbsLifecycle.Invoke, "Script", SupportsShouldProcess = true)]
    [OutputType(typeof (object))]
    public class InvokeScriptCommand : BaseShellCommand
    {
        private const string ParameterSetNameFromItem = "Item";
        private const string ParameterSetNameFromFullPath = "Path";

        [Parameter(ParameterSetName = ParameterSetNameFromItem, ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true, Mandatory = true, Position = 0)]
        public Item Item { get; set; }

        [Parameter(ParameterSetName = ParameterSetNameFromFullPath, Mandatory = true, Position = 0)]
        [Alias("FullName", "FileName")]
        public string Path { get; set; }

        [Parameter]
        public object[] ArgumentList { get; set; }

        // Methods
        protected override void ProcessRecord()
        {
            var script = string.Empty;
            var scriptItem = Item;
            if (Item != null)
            {
                if (!IsPowerShellScriptItem(Item))
                {
                    return;
                }
                script = Item[Templates.Script.Fields.ScriptBody];
            }
            else if (Path != null)
            {
                var drive = IsCurrentDriveSitecore ? CurrentDrive : ApplicationSettings.ScriptLibraryDb;

                scriptItem = PathUtilities.GetItem(Path, drive, ApplicationSettings.ScriptLibraryPath);

                if (scriptItem == null)
                {
                    WriteError(typeof(ItemNotFoundException), $"The script '{Path}' cannot be found.", 
                        ErrorIds.ItemNotFound, ErrorCategory.ObjectNotFound, Path);
                    return;
                }
                if (!IsPowerShellScriptItem(scriptItem))
                {
                    return;
                }
                script = scriptItem[Templates.Script.Fields.ScriptBody];
            }

            if (!ShouldProcess(scriptItem.GetProviderPath(), "Invoke script")) return;
            
            var sendToPipeline = InvokeCommand.InvokeScript(script, false,
                PipelineResultTypes.Output | PipelineResultTypes.Error, null, ArgumentList);

            if (sendToPipeline != null && sendToPipeline.Any())
            {
                WriteObject(sendToPipeline);
            }
        }

        public static string[] ScriptPathsForCompletion()
        {
            var db = Factory.GetDatabase(ApplicationSettings.ScriptLibraryDb);
            if (db == null) return Array.Empty<string>();

            var root = db.GetItem(ApplicationSettings.ScriptLibraryPath);
            if (root == null) return Array.Empty<string>();

            var rootPath = root.Paths.FullPath.TrimEnd('/');
            var results = new List<string>(1024);

            foreach (var script in EnumerateScripts(root))
            {
                var full = script.Paths.FullPath;
                if (!full.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase)) continue;
                var relative = full.Substring(rootPath.Length).TrimStart('/').Replace('/', '\\');
                if (relative.Length > 0) results.Add(relative);
            }

            results.Sort(StringComparer.OrdinalIgnoreCase);
            return results.ToArray();
        }

        private static IEnumerable<Item> EnumerateScripts(Item parent)
        {
            foreach (Item child in parent.GetChildren(ChildListOptions.SkipSorting))
            {
                if (child.TemplateID == Templates.Script.Id) yield return child;
                foreach (var nested in EnumerateScripts(child)) yield return nested;
            }
        }
    }
}