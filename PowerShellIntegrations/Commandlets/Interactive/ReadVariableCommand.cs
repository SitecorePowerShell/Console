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

        protected override void ProcessRecord()
        {
            LogErrors(() =>
            {
                var message = new ShowMultiValuePromptMessage(Parameters, WidthString, HeightString, Title, Description);

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
                        variable = ((PSObject) result["Variable"]).BaseObject as PSVariable;
                    }

                    if (variable != null)
                    {
                        object varValue = result["Value"];
                        if (varValue == null)
                        {
                            result.Add("Value", variable.Value);
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
                }

                JobContext.MessageQueue.PutMessage(message);
                var results = (object[])JobContext.MessageQueue.GetResult();
                foreach (Hashtable result in results)
                {
                    SessionState.PSVariable.Set((string)result["Name"],result["Value"]);
                }
            });
        }
    }
}