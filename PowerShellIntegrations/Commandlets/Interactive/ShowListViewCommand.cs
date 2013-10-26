using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    [Cmdlet(VerbsCommon.Show, "ListView")]
    [OutputType(new[] {typeof (string)})]
    public class ShowListViewCommand : BaseFormCommand
    {
        private readonly List<SvlDataObject> cumulativeData = new List<SvlDataObject>();

        [Parameter(ValueFromPipeline = true, Mandatory = true)]
        public object Data { get; set; }

        [Parameter]
        public object[] Property { get; set; }

        [Parameter]
        public int PageSize { get; set; }

        [Parameter]
        public string Icon { get; set; }

        [Parameter]
        public string InfoTitle { get; set; }

        [Parameter]
        public string InfoDescription { get; set; }

        [Parameter]
        public SwitchParameter Modal { get; set; }

        protected override void BeginProcessing()
        {
            if (Property != null)
            {
                SessionState.PSVariable.Set("ScPsSlvProperties", Property);
            }
            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            SessionState.PSVariable.Set("ScPsSlvPipelineObject", Data);

            if (Property == null && SessionState.PSVariable.Get("ScPsSlvProperties") == null)
            {
                ScriptBlock propScript =
                    InvokeCommand.NewScriptBlock(
                        "$ScPsSlvPipelineObject | foreach-object { $_.PSStandardMembers.DefaultDisplayProperty }");
                PSObject propDefault = InvokeCommand.InvokeScript(SessionState, propScript).First();
                propScript =
                    InvokeCommand.NewScriptBlock(
                        "$ScPsSlvPipelineObject | foreach-object { $_.PSStandardMembers.DefaultDisplayPropertySet.ReferencedPropertyNames }");
                Collection<PSObject> propResult = InvokeCommand.InvokeScript(SessionState, propScript);
                var properties = new List<object>(propResult.Count + 1);
                properties.Add(propDefault.ToString());
                if (propResult.Count() > 0)
                {
                    properties.AddRange(propResult.Where(p => p != null).Cast<object>());
                }
                Property = properties.ToArray();
                SessionState.PSVariable.Set("ScPsSlvProperties", Property);
            }

            ScriptBlock script =
                InvokeCommand.NewScriptBlock("$ScPsSlvPipelineObject | select-object -Property $ScPsSlvProperties");
            Collection<PSObject> result = InvokeCommand.InvokeScript(SessionState, script);

            if (result.Count() > 0)
            {
                object varValue = Data;
                while (varValue is PSObject)
                {
                    varValue = ((PSObject) varValue).ImmediateBaseObject;
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
                int pageSize = PageSize == 0 ? 25 : PageSize;
                if (Data != null)
                {
                    PutMessage(new ShowListViewMessage(cumulativeData, pageSize, Title ?? "PowerShell Script Results",
                        Icon,
                        WidthString, HeightString, Modal.IsPresent, InfoTitle, InfoDescription, Property));
                    FlushMessages();
                }
                SessionState.PSVariable.Remove("$ScPsSlvProperties");
            });
        }

        public class SvlDataObject
        {
            public SvlDataObject(int capacity)
            {
                Display = new Dictionary<string, string>(capacity, StringComparer.OrdinalIgnoreCase);
            }

            public object Original { get; set; }
            public Dictionary<string, string> Display { get; set; }
            public int Id { get; internal set; }
        }
    }
}