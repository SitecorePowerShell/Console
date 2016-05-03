using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;

namespace Cognifide.PowerShell.Commandlets.Interactive
{
    public class BaseListViewCommand : BaseFormCommand
    {
        protected readonly List<DataObject> CumulativeData = new List<DataObject>();

        [Parameter(ValueFromPipeline = true, Mandatory = true)]
        public object Data { get; set; }

        [Parameter]
        public object[] Property { get; set; }

        private Hashtable[] processedProperty;

        protected Hashtable[] ProcessedProperty
        {
            get
            {
                if (processedProperty == null && Property != null)
                {
                    processedProperty = Property.Select(p =>
                    {
                        string label;
                        ScriptBlock expression;

                        if (p is Hashtable)
                        {
                            var h = p as Hashtable;
                            if (h.ContainsKey("Name"))
                            {
                                if (!h.ContainsKey("Label"))
                                {
                                    h.Add("Label", h["Name"]);
                                }
                            }
                            label = h["Label"].ToString();
                            expression = h["Expression"] as ScriptBlock ?? ScriptBlock.Create(h["Expression"].ToString());
                        }
                        else
                        {
                            label = p.ToString();                            
                            expression = ScriptBlock.Create($"$ofs=', ';\"$($_.'{label}')\"");
                        }
                        var result = new Hashtable(2)
                        {
                            {"Label", label},
                            {"Expression", expression}
                        };
                        return result;
                    }).ToArray();
                }
                return processedProperty;
            }
        }

        protected override void BeginProcessing()
        {
            if (Property == null && SessionState.PSVariable.Get("formatProperty") != null)
            {
                SessionState.PSVariable.Set("ScPsSlvProperties", SessionState.PSVariable.Get("formatPropertyStr").Value);
            }
            else if (Property != null)
            {
                SessionState.PSVariable.Set("ScPsSlvProperties", ProcessedProperty);
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
                var propScript =
                    InvokeCommand.NewScriptBlock(
                        "$ScPsSlvPipelineObject | foreach-object { $_.PSStandardMembers.DefaultDisplayProperty }");
                var propDefault = InvokeCommand.InvokeScript(SessionState, propScript).First();
                if (propDefault != null)
                {
                    propScript =
                        InvokeCommand.NewScriptBlock(
                            "$ScPsSlvPipelineObject | foreach-object { $_.PSStandardMembers.DefaultDisplayPropertySet.ReferencedPropertyNames }");
                    var propResult = InvokeCommand.InvokeScript(SessionState, propScript);
                    var properties = new List<object>(propResult.Count + 1) {propDefault.ToString()};
                    if (propResult.Any())
                    {
                        properties.AddRange(propResult.Where(p => p != null));
                    }
                    Property = properties.ToArray();
                    SessionState.PSVariable.Set("ScPsSlvProperties", Property);
                }
            }

            LogErrors(() =>
            {
                var script = (Property == null && SessionState.PSVariable.Get("formatPropertyStr") != null)
                    ? "$ScPsSlvPipelineObject | select-object -Property " +
                      SessionState.PSVariable.Get("formatPropertyStr").Value
                    : "$ScPsSlvPipelineObject | select-object -Property $ScPsSlvProperties";

                var scriptBlock = InvokeCommand.NewScriptBlock(script);
                var result = InvokeCommand.InvokeScript(SessionState, scriptBlock);

                if (result.Any())
                {
                    var varValue = Data.BaseObject();

                    var slvDataObject = new DataObject
                    {
                        Original = varValue,
                        Id = CumulativeData.Count
                    };

                    if (Property == null)
                    {
                        //last effort to recover property list for further reuse
                        Property = result[0]?.Properties?.Select(resultItem => resultItem.Name).Cast<object>().ToArray();
                    }
                    foreach (var psPropertyInfo in result[0].Properties)
                    {
                        slvDataObject.Display.Add(psPropertyInfo.Name, (psPropertyInfo.Value ?? string.Empty).ToString());
                    }
                    CumulativeData.Add(slvDataObject);
                }
            });
            SessionState.PSVariable.Remove("ScPsSlvPipelineObject");
        }

        protected override void EndProcessing()
        {
            base.EndProcessing();
            LogErrors(() => SessionState.PSVariable.Remove("ScPsSlvProperties"));
        }

        [Serializable]
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