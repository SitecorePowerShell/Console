using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    public class BaseListViewCommand : BaseFormCommand
    {
        protected readonly List<DataObject> cumulativeData = new List<DataObject>();

        [Parameter(ValueFromPipeline = true, Mandatory = true)]
        public object Data { get; set; }

        [Parameter]
        public object[] Property { get; set; }

        protected override void BeginProcessing()
        {
            if (Property == null && SessionState.PSVariable.Get("formatProperty") != null)
            {
                //Property = SessionState.PSVariable.Get("formatProperty").Value as object[];
                SessionState.PSVariable.Set("ScPsSlvProperties", SessionState.PSVariable.Get("formatPropertyStr").Value);
            }
            else if (Property != null)
            {
                SessionState.PSVariable.Set("ScPsSlvProperties", Property);
            }
            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            while ((Data is PSObject) && (Data as PSObject).ImmediateBaseObject is PSObject)
            {
                Data = (Data as PSObject).ImmediateBaseObject;
            }
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

            LogErrors(() =>
            {
                string script = (Property == null && SessionState.PSVariable.Get("formatPropertyStr") != null)
                    ? "$ScPsSlvPipelineObject | select-object -Property " +
                      SessionState.PSVariable.Get("formatPropertyStr").Value
                    : "$ScPsSlvPipelineObject | select-object -Property $ScPsSlvProperties";

                ScriptBlock scriptBlock = InvokeCommand.NewScriptBlock(script);
                Collection<PSObject> result = InvokeCommand.InvokeScript(SessionState, scriptBlock);

                if (result.Count() > 0)
                {
                    object varValue = Data;
                    while (varValue is PSObject)
                    {
                        varValue = ((PSObject) varValue).ImmediateBaseObject;
                    }

                    var slvDataObject = new DataObject
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
            });
            SessionState.PSVariable.Remove("ScPsSlvPipelineObject");
        }

        protected override void EndProcessing()
        {
            base.EndProcessing();
            LogErrors(() => SessionState.PSVariable.Remove("ScPsSlvProperties"));
        }

        public class DataObject
        {
            public DataObject()
            {
                Display = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            public object Original { get; set; }
            public Dictionary<string, string> Display { get; set; }
            public int Id { get; internal set; }
        }
    }
}