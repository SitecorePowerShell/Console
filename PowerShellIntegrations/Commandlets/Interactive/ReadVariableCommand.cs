using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    [Cmdlet("Read", "Variable")]
    [OutputType(new[] {typeof (string)})]
    public class ReadVariableCommand : BaseFormCommand
    {
        [Parameter]
        public object[] Parameters { get; set; }

        [Parameter]
        public string Description { get; set; }

        [Parameter]
        public string CancelButtonName { get; set; }

        [Parameter]
        public string OkButtonName { get; set; }

        public ReadVariableCommand()
        {
            Width = 500;
            Height = 300;
        }

        protected override void ProcessRecord()
        {
            LogErrors(() =>
            {
                var message = new ShowMultiValuePromptMessage(Parameters, WidthString, HeightString, Title, Description,
                    OkButtonName, CancelButtonName);

                foreach (Hashtable result in Parameters)
                {
                    var name = result["Name"] as string;

                    PSVariable variable = null;
                    if (!string.IsNullOrEmpty(name))
                    {
                        variable = SessionState.PSVariable.Get((string) result["Name"]);
                    }

                    if (result["Variable"] != null)
                    {
                        variable = (PSVariable) ((PSObject) result["Variable"]).BaseObject;
                        result.Add("Name", variable.Name);
                        name = variable.Name;
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

                            if (varValue is IEnumerable<object>)
                            {
                                varValue = (varValue as IEnumerable<object>).Select(p =>
                                {
                                    while (p is PSObject)
                                    {
                                        p = ((PSObject) p).ImmediateBaseObject;
                                    }
                                    return p;
                                }).ToList();
                            }
                            result.Add("Value", varValue);
                        }
                        object varTitle = result["Title"];
                        if (varTitle == null)
                        {
                            result.Add("Title", string.IsNullOrEmpty(variable.Name) ? name : variable.Name);
                        }
                        object varDesc = result["Description"];
                        if (varDesc == null)
                        {
                            result.Add("Description", variable.Description);
                        }
                    }

                    if (result["Value"] == null)
                    {
                        if (result.ContainsKey("Value"))
                        {
                            result["Value"] = string.Empty;
                        }
                        else
                        {
                            result.Add("Value", string.Empty);
                        }
                    }
                }

                PutMessage(message);
                var results = (object[]) GetResult(message);
                WriteObject(results != null ? "ok" : "cancel");
                if (results != null)
                {
                    foreach (Hashtable result in results)
                    {
                        object resultValue = result["Value"];
                        if (resultValue is Item)
                        {
                            resultValue = ItemShellExtensions.GetPsObject(SessionState, resultValue as Item);
                        }
                        if (resultValue is List<Item>)
                        {
                            resultValue =
                                (resultValue as List<Item>).Select(p => ItemShellExtensions.GetPsObject(SessionState, p))
                                    .ToArray();
                        }
                        SessionState.PSVariable.Set((string) result["Name"], resultValue);
                    }
                }
            });
        }
    }
}