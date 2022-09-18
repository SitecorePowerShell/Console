using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Data;
using Sitecore.Data.Items;
using Spe.Commands.Interactive;
using Spe.Core.Diagnostics;
using Spe.Core.Modules;
using Spe.Core.Utility;
using Spe.Core.Validation;
using Spe.Core.Settings;

namespace Spe.Commands.Session
{
    [Cmdlet(VerbsData.Import, "Function", SupportsShouldProcess = true)]
    [OutputType(typeof (object))]
    public class ImportFunctionCommand : BaseShellCommand
    {
        class FunctionCacheEntry
        {
            internal string Name { get; set; }
            internal string Library { get; set; }
            internal string Module { get; set; }
            internal string Database { get; set; }
            internal ID ScriptID { get; set; }
        }

        private static string[] functions;
        private static string[] libraries;
        private static string[] modules;
        private static List<FunctionCacheEntry> FunctionCache { get; set; }


        [Parameter(Mandatory = true, Position = 0)]
        [AutocompleteSet(nameof(Functions))]
        public string Name { get; set; }

        [Parameter]
        [AutocompleteSet(nameof(Libraries))]
        public string Library { get; set; }

        [Parameter]
        [AutocompleteSet(nameof(Modules))]
        public string Module { get; set; }

        public static string[] Functions
        {
            get
            {
                if (FunctionCache == null)
                {
                    UpdateCache();
                }

                if (functions == null)
                {
                    functions = FunctionCache.
                        Select(f => f.Name).
                        Distinct().
                        OrderBy(s => s).
                        Select(s => WrapNameWithSpacesInQuotes(s)).
                        ToArray();
                }
                return functions;
            }
        }

        public static string[] Libraries
        {
            get
            {
                if (FunctionCache == null)
                {
                    UpdateCache();
                }

                if (libraries == null)
                {
                    libraries = FunctionCache.
                        Select(f => f.Library).
                        Distinct().
                        Where(s => s != string.Empty).
                        OrderBy(s => s).
                        Select(s => WrapNameWithSpacesInQuotes(s)).ToArray();
                }
                return libraries;
            }
        }

        public static string[] Modules
        {
            get
            {
                if (FunctionCache == null)
                {
                    UpdateCache();
                }

                if (libraries == null)
                {
                    modules = FunctionCache.
                        Select(f => f.Module).
                        Distinct().
                        OrderBy(s => s).
                        Select(s => WrapNameWithSpacesInQuotes(s)).
                        ToArray();
                }
                return modules;
            }
        }


        static ImportFunctionCommand()
        {
            ModuleManager.OnInvalidate += InvalidateCache;
            FunctionCache = null;
            libraries = null;
            functions = null;
            modules = null;
        }

        // Methods
        protected override void ProcessRecord()
        {
            if (FunctionCache == null)
            {
                UpdateCache();
            }

            if (string.IsNullOrEmpty(Name))
            {
                WriteError(new ErrorRecord(new AmbiguousMatchException(
                    "Function name was not provided."),
                    "sitecore_name_missing", ErrorCategory.NotSpecified, null));
                return;
            }

            var filteredFunctions = FunctionCache.Where(f => string.Equals(f.Name, Name, StringComparison.InvariantCultureIgnoreCase)).ToList();

            if (!string.IsNullOrEmpty(Module))
            {
                filteredFunctions = filteredFunctions.Where(f => string.Equals(f.Module, Module, StringComparison.InvariantCultureIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(Library))
            {
                filteredFunctions = filteredFunctions.Where(f => f.Library.StartsWith(Library, StringComparison.InvariantCultureIgnoreCase)).ToList();
            }
            
            var roots = ModuleManager.GetFeatureRoots(IntegrationPoints.FunctionsFeature);

            if (filteredFunctions.Count > 1)
            {
                WriteError(new ErrorRecord(new AmbiguousMatchException(
                        $"Ambiguous function name '{Name}' detected, please narrow your search by specifying sub-library and/or module name."),
                    "sitecore_ambiguous_name", ErrorCategory.InvalidData, null));
                return;
            }

            if (filteredFunctions.Count == 0)
            {
                WriteError(new ErrorRecord(new AmbiguousMatchException(
                        $"Function item with name '{Name}' could not be found in the specified module or library or it does not exist."),
                    "sitecore_function_not_found", ErrorCategory.ObjectNotFound, null));
                return;
            }

            var functionItem = Factory.GetDatabase(ApplicationSettings.ScriptLibraryDb).GetItem(filteredFunctions[0].ScriptID);
            
            if (!IsPowerShellScriptItem(functionItem))
            {
                // this should never happen as cache only stores Scripts
                return;
            }

            var script = functionItem[Templates.Script.Fields.ScriptBody];

            if (ShouldProcess(functionItem.GetProviderPath(), "Import functions"))
            {
                var sendToPipeline = InvokeCommand.InvokeScript(script, false,
                    PipelineResultTypes.Output | PipelineResultTypes.Error, null);

                if (sendToPipeline != null && sendToPipeline.Any())
                {
                    WriteObject(sendToPipeline);
                }
            }

        }

        private static void UpdateCache()
        {
            var functionCache = new List<FunctionCacheEntry>();
            var roots = ModuleManager.GetFeatureRoots(IntegrationPoints.FunctionsFeature);

            foreach (var root in roots)
            {
                try
                {
                    // One does not simply Select... as modules can have more than Query.MaxItems
                    // We have to traverse the tree.
                    IEnumerable<Item> results = GetAllScriptChildren(root);
                    functionCache.AddRange(results.Select(p =>
                     new FunctionCacheEntry() { 
                        Name = p.Name,
                        Library = GetLibraryName(p.Parent, string.Empty),
                        Database = p.Database.Name,
                        Module = GetModuleName(p.Parent),
                        ScriptID = p.ID
                    }));
                }
                catch (Exception ex)
                {
                    PowerShellLog.Error("Error while querying for items", ex);
                }
            } 

            FunctionCache = functionCache;
            libraries = null;
            functions = null;
            modules = null;
        }

        private static IEnumerable<Item> GetAllScriptChildren(Item parent)
        {            
            foreach (Item child in parent.GetChildren(ChildListOptions.SkipSorting))
            {
                if (child.TemplateID == Templates.Script.Id)
                {
                    yield return child;
                }
                foreach (Item subChild in GetAllScriptChildren(child))
                {
                    yield return subChild;
                }
            }
        }

        private static string GetModuleName(Item item)
        {
            return item.TemplateID != Templates.ScriptModule.Id ? GetModuleName(item.Parent) : item.Name;
        }

        private static string GetLibraryName(Item item, string pathSoFar)
        {
            return item.TemplateID == Templates.ScriptLibrary.Id && item.Name != "Functions"
                ? GetLibraryName(item.Parent, $"{item.Name}\\{pathSoFar}") 
                : pathSoFar.Trim('\\');
        }

        public static void InvalidateCache(object sender, EventArgs e)
        {
            FunctionCache = null;
        }
    }
}