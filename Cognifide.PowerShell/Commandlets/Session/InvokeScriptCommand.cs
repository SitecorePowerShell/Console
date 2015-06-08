using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Cognifide.PowerShell.Commandlets.Interactive;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Session
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

        // Methods
        protected override void ProcessRecord()
        {
            var script = string.Empty;
            var scriptItem = Item;
            if (Item != null)
            {
                script = Item[ScriptItemFieldNames.Script];
            }
            else if (Path != null)
            {
                var drive = IsCurrentDriveSitecore ? CurrentDrive : ApplicationSettings.ScriptLibraryDb;

                scriptItem = PathUtilities.GetItem(Path, drive, ApplicationSettings.ScriptLibraryPath);

                if (scriptItem == null)
                {
                    var error = String.Format("The script '{0}' is cannot be found.", Path);
                    WriteError(new ErrorRecord(new ItemNotFoundException(error), error, ErrorCategory.ObjectNotFound, Path));
                    return;
                }
                script = scriptItem[ScriptItemFieldNames.Script];
            }
            if (!ShouldProcess(scriptItem.GetProviderPath(), "Invoke script")) return;
            
            object sendToPipeline = InvokeCommand.InvokeScript(script, false,
                PipelineResultTypes.Output | PipelineResultTypes.Error, null);
            WriteObject(sendToPipeline);
        }
    }
}