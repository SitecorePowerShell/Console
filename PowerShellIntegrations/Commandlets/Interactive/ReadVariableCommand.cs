using System.Collections;
using System.Linq;
using System.Management.Automation;
using System.Security.Policy;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Web;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    [Cmdlet("Read", "Variable")]
    public class ReadVariableCommand : BaseFormCommand
    {
        [Parameter]
        public object[] Parameters{ get; set; }

        [Parameter]
        public string Description { get; set; }

        [Parameter]
        public string CancelButtonName { get; set; }
        
        [Parameter]
        public string OkButtonName { get; set; }

        protected override void ProcessRecord()
        {
            LogErrors(() =>
            {
                var message = new ShowMultiValuePromptMessage(Parameters, WidthString, HeightString, Title, Description, OkButtonName, CancelButtonName);

                foreach (Hashtable result in Parameters)
                {
                    string name = result["Name"] as string;

                    PSVariable variable = null;
                    if (!string.IsNullOrEmpty(name))
                    {
                        variable = SessionState.PSVariable.Get((string) result["Name"]);
                    }

                    if (result["Variable"] != null)
                    {
                        variable = (PSVariable)((PSObject)result["Variable"]).BaseObject;
                        result.Add("Name",variable.Name);
                    }

                    if (variable != null)
                    {
                        object varValue = result["Value"];
                        if (varValue == null)
                        {
                            varValue = variable.Value;
                            while (varValue is PSObject)
                            {
                                varValue = ((PSObject) varValue).ImmediateBaseObject;
                            }
                            result.Add("Value", varValue);
                        }
                        object varTitle = result["Title"];
                        if (varTitle == null)
                        {
                            result.Add("Title", variable.Name);
                        }
                        object varDesc = result["Description"];
                        if (varDesc == null)
                        {
                            result.Add("Description", variable.Description);
                        }
                    }

                    if (result["Value"] == null)
                    {
                        result.Add("Value", string.Empty);
                    }
                }

                JobContext.MessageQueue.PutMessage(message);
                var results = (object[])JobContext.MessageQueue.GetResult();
                WriteObject(results != null ? "ok" : "cancel");
                if (results != null)
                {
                    foreach (Hashtable result in results)
                    {
                        SessionState.PSVariable.Set((string) result["Name"], result["Value"]);
                    }
                }
            });
        }
    }
}