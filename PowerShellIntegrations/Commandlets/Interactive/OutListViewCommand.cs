using System;
using System.Collections;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Text;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Web;
using Sitecore.Web.Authentication;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    [Cmdlet("Out", "ListView")]
    [OutputType(new[] { typeof(string) })]
    public class OutListViewCommand : BaseFormCommand
    {
        [Parameter(ParameterSetName = "Text Results")]
        public SwitchParameter Text { get; set; }

        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject { get; set; }

        public PSObject Parameters { get; set; }

        public string[] Property { get; set; }

/*
        protected override void ProcessRecord()
        {
            LogErrors(() =>
                {
                    string response = string.Empty;
                    {
                        var hashParams =
                            new Hashtable(Parameters.ToDictionary(p => p.ToString().Split('|')[0],
                                                                  p => WebUtil.SafeEncode(p.ToString().Split('|')[1])));
                        response = JobContext.ShowModalDialog(hashParams, Control, WidthString, HeightString);
                    }
                    WriteObject(response);
                });
        }
*/

        private void ProcessObject(PSObject input)
        {
/*
            if (this.WindowProxy.IsWindowClosed())
            {

                LocalPipeline currentlyRunningPipeline = (LocalPipeline)base.Context.CurrentRunspace.GetCurrentlyRunningPipeline();
                if ((currentlyRunningPipeline != null) && !currentlyRunningPipeline.IsStopping)
                {
                    currentlyRunningPipeline.StopAsync();
                }
            }
            else
            {
*/
            object baseObject = input.BaseObject;
        }

        protected override void ProcessRecord()
        {
            if ((InputObject != null) && !Equals(InputObject, AutomationNull.Value))
            {
                var baseObject = InputObject.BaseObject as IDictionary;
                if (baseObject != null)
                {
                    foreach (DictionaryEntry entry in baseObject)
                    {
                        ProcessObject(PSObject.AsPSObject(entry));
                    }
                }
                else
                {
                    ProcessObject(InputObject);
                }
            }
        }

    }
}