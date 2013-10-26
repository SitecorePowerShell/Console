using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive;
using Sitecore;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets
{
    [Cmdlet("Import", "Function")]
    [OutputType(new[] {typeof (object)})]
    public class ImportFunctionCommand : BaseShellCommand
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string Name { get; set; }

        [Parameter(Position = 1)]
        public string Library { get; set; }

        // Methods
        protected override void ProcessRecord()
        {
            string script = string.Empty;

            if (Name != null)
            {
                Item[] functions;

                if (string.IsNullOrEmpty(Library))
                {
                    functions = Context.ContentDatabase.SelectItems(
                        string.Format(
                            "/sitecore/system/Modules/PowerShell/#Script Library#/Functions//*[@@TemplateId=\"{{DD22F1B3-BD87-4DB2-9E7D-F7A496888D43}}\" and @@Name=\"{0}\"]",
                            Name));
                }
                else
                {
                    functions = Context.ContentDatabase.SelectItems(
                        string.Format(
                            "/sitecore/system/Modules/PowerShell/#Script Library#/Functions/#{0}#//*[@@TemplateId=\"{{DD22F1B3-BD87-4DB2-9E7D-F7A496888D43}}\" and @@Name=\"{1}\"]",
                            Library, Name));
                }

                if (functions.Length > 1)
                {
                    throw new AmbiguousMatchException(
                        string.Format(
                            "Ambiguous function name '{0}'detected, please narrow your search by specifying library.",
                            Name));
                }

                script = functions[0]["script"];
                object sendToPipeline = InvokeCommand.InvokeScript(script, false,
                    PipelineResultTypes.Output | PipelineResultTypes.Error, null, new object[0]);
                WriteObject(sendToPipeline);
            }
        }
    }
}