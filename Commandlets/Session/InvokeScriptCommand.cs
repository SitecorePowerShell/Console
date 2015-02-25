using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Cognifide.PowerShell.Commandlets.Interactive;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Session
{
    [Cmdlet(VerbsLifecycle.Invoke, "Script", SupportsShouldProcess = true)]
    [OutputType(new[] {typeof (object)})]
    public class InvokeScriptCommand : BaseShellCommand
    {
        private const string ParameterSetNameFromItem = "From Item";
        private const string ParameterSetNameFromFullPath = "From Full Path";

        [Parameter(ParameterSetName = ParameterSetNameFromItem, ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true, Mandatory = true, Position = 0)]
        public Item Item { get; set; }

        [Parameter(ParameterSetName = ParameterSetNameFromFullPath, Mandatory = true, Position = 0)]
        [Alias("FullName", "FileName")]
        public string Path { get; set; }

        // Methods
        protected override void ProcessRecord()
        {
            string script = string.Empty;
            Item scriptItem = Item;
            if (Item != null)
            {
                script = Item["script"];
            }
            else if (Path != null)
            {
                var drive = IsCurrentDriveSitecore ? CurrentDrive : ApplicationSettings.ScriptLibraryDb;

                scriptItem = PathUtilities.GetItem(Path, drive, ApplicationSettings.ScriptLibraryPath);

                if (scriptItem == null)
                {
                    WriteError(new ErrorRecord(
                        new ItemNotFoundException(string.Format("Script '{0}' not found.", Path)),
                        "sitecore_script_missing", ErrorCategory.ObjectNotFound, null));
                    return;
                }
                script = scriptItem["script"];
            }
            if (ShouldProcess(scriptItem.GetProviderPath(), "Invoke script"))
            {

                object sendToPipeline = InvokeCommand.InvokeScript(script, false,
                    PipelineResultTypes.Output | PipelineResultTypes.Error, null, new object[0]);
                WriteObject(sendToPipeline);
            }
        }
    }
}