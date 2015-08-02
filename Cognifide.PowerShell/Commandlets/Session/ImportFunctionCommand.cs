using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using Cognifide.PowerShell.Commandlets.Interactive;
using Cognifide.PowerShell.Core.Modules;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Cognifide.PowerShell.Core.Validation;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;

namespace Cognifide.PowerShell.Commandlets.Session
{
    [Cmdlet(VerbsData.Import, "Function", SupportsShouldProcess = true)]
    [OutputType(typeof (object))]
    public class ImportFunctionCommand : BaseShellCommand
    {
        private static string[] Functions;
        private static string[] Libraries;
        private static string[] Modules;

        [Parameter(Mandatory = true, Position = 0)]
        [AutocompleteSet("Functions")]
        public string Name { get; set; }

        [Parameter]
        [AutocompleteSet("Libraries")]
        public string Library { get; set; }

        [Parameter]
        [AutocompleteSet("Modules")]
        public string Module { get; set; }


        public override object GetDynamicParameters()
        {
            if (Functions == null)
            {
                UpdateCache();
            }

            return base.GetDynamicParameters();
        }

        static ImportFunctionCommand()
        {
            ModuleManager.OnInvalidate += InvalidateCache;
            Functions = null;
        }

        // Methods
        protected override void ProcessRecord()
        {
            if (string.IsNullOrEmpty(Name))
            {
                WriteError(new ErrorRecord(new AmbiguousMatchException(
                    "Function name was not provided."),
                    "sitecore_name_missing", ErrorCategory.NotSpecified, null));
                return;
            }

            var functionItems = new List<Item>();
            var roots = ModuleManager.GetFeatureRoots(IntegrationPoints.FunctionsFeature);

            if (!string.IsNullOrEmpty(Module))
            {
                roots =
                    roots.Where(
                        p =>
                            string.Equals(ModuleManager.GetItemModule(p).Name, Module,
                                StringComparison.InvariantCultureIgnoreCase)).ToList();
            }

            foreach (var root in roots)
            {
                var path = PathUtilities.PreparePathForQuery(root.Paths.Path);
                if (string.IsNullOrEmpty(Library))
                {
                    functionItems.AddRange(root.Database.SelectItems(
                        string.Format(
                            "{0}//*[@@TemplateId=\"{{DD22F1B3-BD87-4DB2-9E7D-F7A496888D43}}\" and @@Name=\"{1}\"]",
                            path, Name)));
                }
                else
                {
                    functionItems.AddRange(root.Database.SelectItems(
                        string.Format(
                            "{0}/#{1}#//*[@@TemplateId=\"{{DD22F1B3-BD87-4DB2-9E7D-F7A496888D43}}\" and @@Name=\"{2}\"]",
                            path, Library, Name)));
                }
            }

            if (functionItems.Count > 1)
            {
                WriteError(new ErrorRecord(new AmbiguousMatchException(
                    string.Format(
                        "Ambiguous function name '{0}' detected, please narrow your search by specifying sub-library and/or module name.",
                        Name)), "sitecore_ambiguous_name", ErrorCategory.InvalidData, null));
                return;
            }

            if (functionItems.Count == 0)
            {
                WriteError(new ErrorRecord(new AmbiguousMatchException(
                    string.Format(
                        "Function item with name '{0}' could not be found in the specified module or library or it does not exist.",
                        Name)), "sitecore_function_not_found", ErrorCategory.ObjectNotFound, null));
                return;
            }

            var script = functionItems[0][ScriptItemFieldNames.Script];
            if (ShouldProcess(functionItems[0].GetProviderPath(), "Import functions"))
            {
                object sendToPipeline = InvokeCommand.InvokeScript(script, false,
                    PipelineResultTypes.Output | PipelineResultTypes.Error, null);
                WriteObject(sendToPipeline);
            }

        }

        private static void UpdateCache()
        {
            var localFunctions = new List<string>();
            var roots = ModuleManager.GetFeatureRoots(IntegrationPoints.FunctionsFeature);

            foreach (var root in roots)
            {
                var path = PathUtilities.PreparePathForQuery(root.Paths.Path);
                var query = string.Format(
                    "{0}//*[@@TemplateId=\"{{DD22F1B3-BD87-4DB2-9E7D-F7A496888D43}}\"]",
                    path);
                try
                {
                    var results = root.Database.SelectItems(query);
                    localFunctions.AddRange(results.ToList().ConvertAll(p => p.Name));
                }
                catch (Exception ex)
                {
                    Log.Error("Error while querying for items", ex);
                }
            }
            Functions = localFunctions.ToArray();

            Libraries = (from root in roots
                from Item library in root.GetChildren()
                where library.TemplateName == TemplateNames.ScriptLibraryTemplateName
                select library.Name).ToArray();

            Modules = (from module in ModuleManager.Modules where module.Enabled select module.Name).ToArray();
        }

        public static void InvalidateCache(object sender, EventArgs e)
        {
            Functions = null;
        }
    }
}