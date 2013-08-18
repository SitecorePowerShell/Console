using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages;
using Sitecore.Data.Items;
using Sitecore.Jobs.AsyncUI;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets
{
    [Cmdlet("Execute", "Script")]
    [OutputType(new[] { typeof(object) })]
    public class ExecuteScriptCommand : BaseShellCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "From Item", Mandatory = true, Position = 0)]
        public Item Item { get; set; }

        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "From Path", Mandatory = true, Position = 0)]
        [Alias("FullName", "FileName")]
        public string Path { get; set; }

        // Methods
        protected override void ProcessRecord()
        {
            string script = string.Empty;
            if (Item != null)
            {
                script = Item["script"];
            }
            else if (Path != null)
            {
                var curItem = PathUtilities.GetItem(Path, CurrentDrive, CurrentPath);
                script = curItem["script"];
            }

            object sendToPipeline = InvokeCommand.InvokeScript(script, false, PipelineResultTypes.Output | PipelineResultTypes.Error,null,new object[0]);
            WriteObject(sendToPipeline);
        }
    }
}