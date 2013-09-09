using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Web;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    [Cmdlet(VerbsCommon.Show, "ListView")]
    [OutputType(new[] { typeof(string) })]
    public class ShowListViewCommand : BaseFormCommand
    {
        public class SvlDataObject
        {
            public SvlDataObject(int capacity)
            {
                Display = new Dictionary<string, string>(capacity, StringComparer.OrdinalIgnoreCase);
            }
            public object Original { get; set; }
            public Dictionary<string,string> Display { get; set; }
            public int Id { get; internal set; }
        }

        private List<SvlDataObject> cumulativeData = new List<SvlDataObject>();

        [Parameter(ValueFromPipeline = true, Mandatory = true)]
        public object Data { get; set; }

        [Parameter(Mandatory = true)]
        public object[] Property { get; set; }

        [Parameter]
        public int PageSize { get; set; }

        [Parameter]
        public string Icon { get; set; }

        [Parameter]
        public SwitchParameter Modal { get; set; }

        protected override void BeginProcessing()
        {
            SessionState.PSVariable.Set("ScPsSlvProperties", Property);
            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            SessionState.PSVariable.Set("ScPsSlvPipelineObject", Data);
            ScriptBlock script =
                InvokeCommand.NewScriptBlock("$ScPsSlvPipelineObject | select-object -Property $ScPsSlvProperties");
            Collection<PSObject> result = InvokeCommand.InvokeScript(SessionState, script);

            if (result.Count() > 0)
            {
                var varValue = Data;
                while (varValue is PSObject)
                {
                    varValue = ((PSObject)varValue).ImmediateBaseObject;
                }

                var slvDataObject = new SvlDataObject(Property.Length)
                {
                    Original = varValue,
                    Id = cumulativeData.Count
                };

                foreach (var psPropertyInfo in result[0].Properties)
                {
                    slvDataObject.Display.Add(psPropertyInfo.Name, (psPropertyInfo.Value ?? string.Empty).ToString());
                }
                cumulativeData.Add(slvDataObject);
            }
            SessionState.PSVariable.Remove("$ScPsSlvPipelineObject");
        }

        protected override void EndProcessing()
        {
            base.EndProcessing();
            LogErrors(() =>
            {
                int pageSize = PageSize == 0  ? 20 : PageSize;
                if (Data != null)
                {
                    var message =
                        new ShowListViewMessage(cumulativeData, pageSize, Title ?? "PowerShell Script Results", Icon,
                            WidthString, HeightString, Modal.IsPresent);

                    JobContext.MessageQueue.PutMessage(message);
                    JobContext.Flush();
                }
                SessionState.PSVariable.Remove("$ScPsSlvProperties");
            });
        }

    }
}