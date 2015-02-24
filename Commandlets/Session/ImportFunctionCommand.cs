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
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;

namespace Cognifide.PowerShell.Commandlets.Session
{
    [Cmdlet(VerbsData.Import, "Function")]
    [OutputType(typeof (object))]
    public class ImportFunctionCommand : BaseShellCommand, IDynamicParameters
    {
        private static string[] functions;
        private static string[] libraries;
        private static string[] modules;

        public ImportFunctionCommand()
        {
            if (functions == null)
            {
                UpdateCache();
            }
            AddDynamicParameter<string>("Name", new ParameterAttribute
            {
                ParameterSetName = ParameterAttribute.AllParameterSets,
                Mandatory = true,
                Position = 0
            }, new ValidateSetAttribute(functions));

            var libraryAttributes = new List<Attribute>
            {
                new ParameterAttribute
                {
                    ParameterSetName = ParameterAttribute.AllParameterSets,
                    Mandatory = false,
                    Position = 1
                }
            };
            if (libraries.Length > 0)
            {
                libraryAttributes.Add(new ValidateSetAttribute(libraries));
            }

            AddDynamicParameter<string>("Library", libraryAttributes.ToArray());

            AddDynamicParameter<string>("Module", new ParameterAttribute
            {
                ParameterSetName = ParameterAttribute.AllParameterSets,
                Mandatory = false,
                Position = 2
            }, new ValidateSetAttribute(modules));
        }

        static ImportFunctionCommand()
        {
            ModuleManager.OnInvalidate += InvalidateCache;
            functions = null;
        }

        // Methods
        protected override void ProcessRecord()
        {
            var script = string.Empty;
            string name;
            if (TryGetParameter("Name", out name))
            {
                if (string.IsNullOrEmpty(name))
                {
                    WriteError(new ErrorRecord(new AmbiguousMatchException(
                        "Function name was not provided."),
                        "sitecore_name_missing", ErrorCategory.NotSpecified, null));
                    return;
                }
            }

            if (name != null)
            {
                var functions = new List<Item>();
                var roots = ModuleManager.GetFeatureRoots(IntegrationPoints.FunctionsFeature);

                string module;
                TryGetParameter("Module", out module);

                string library;
                TryGetParameter("Library", out library);

                if (!string.IsNullOrEmpty(module))
                {
                    roots =
                        roots.Where(
                            p =>
                                string.Equals(ModuleManager.GetItemModule(p).Name, module,
                                    StringComparison.InvariantCultureIgnoreCase)).ToList();
                }

                foreach (var root in roots)
                {
                    var path = PathUtilities.PreparePathForQuery(root.Paths.Path);
                    if (string.IsNullOrEmpty(library))
                    {
                        functions.AddRange(root.Database.SelectItems(
                            string.Format(
                                "{0}//*[@@TemplateId=\"{{DD22F1B3-BD87-4DB2-9E7D-F7A496888D43}}\" and @@Name=\"{1}\"]",
                                path, name)));
                    }
                    else
                    {
                        functions.AddRange(root.Database.SelectItems(
                            string.Format(
                                "{0}/#{1}#//*[@@TemplateId=\"{{DD22F1B3-BD87-4DB2-9E7D-F7A496888D43}}\" and @@Name=\"{2}\"]",
                                path, library, name)));
                    }
                }

                if (functions.Count > 1)
                {
                    WriteError(new ErrorRecord(new AmbiguousMatchException(
                        string.Format(
                            "Ambiguous function name '{0}' detected, please narrow your search by specifying sub-library and/or module name.",
                            name)), "sitecore_ambiguous_name", ErrorCategory.InvalidData, null));
                    return;
                }

                if (functions.Count == 0)
                {
                    WriteError(new ErrorRecord(new AmbiguousMatchException(
                        string.Format(
                            "Function item with name '{0}' could not be found in the specified module or library or it does not exist.",
                            name)), "sitecore_function_not_found", ErrorCategory.ObjectNotFound, null));
                    return;
                }

                script = functions[0]["script"];
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
            functions = localFunctions.ToArray();

            var localLibraries = new List<string>();
            foreach (var root in roots)
            {
                foreach (Item library in root.GetChildren())
                {
                    if (library.TemplateName == TemplateNames.ScriptLibraryTemplateName)
                        localLibraries.Add(library.Name);
                }
            }
            libraries = localLibraries.ToArray();

            var localModules = new List<string>();
            foreach (var module in ModuleManager.Modules)
            {
                if (module.Enabled)
                    localModules.Add(module.Name);
            }
            modules = localModules.ToArray();
        }

        public static void InvalidateCache(object sender, EventArgs e)
        {
            functions = null;
        }
    }
}