using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Spe.Core.Extensions;

namespace Spe.Commands.Interactive
{
    public class BaseListViewCommand : BaseFormCommand
    {
        protected readonly List<DataObject> CumulativeData = new List<DataObject>();

        [Parameter]
        public string InfoTitle { get; set; }

        [Parameter]
        public string InfoDescription { get; set; }

        [Parameter]
        public string MissingDataMessage { get; set; }

        [Parameter]
        public string MissingDataIcon { get; set; }

        [Parameter(ValueFromPipeline = true, Mandatory = true)]
        public object Data { get; set; }

        [Parameter]
        public object[] Property { get; set; }

        private Hashtable[] _processedProperty;

        protected Hashtable[] ProcessedProperty
        {
            get
            {
                if (_processedProperty == null && Property != null)
                {
                    _processedProperty = Property.Select(p =>
                    {
                        string label;
                        ScriptBlock expression;

                        if (p is Hashtable h)
                        {
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
                return _processedProperty;
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
            while ((Data as PSObject)?.ImmediateBaseObject is PSObject)
            {
                Data = (Data as PSObject).ImmediateBaseObject;
            }
            SessionState.PSVariable.Set("ScPsSlvPipelineObject", Data);

            if (Property == null && SessionState.PSVariable.Get("ScPsSlvProperties") == null)
            {
                var hasCustomObjects = false;
                var propScript =
                    InvokeCommand.NewScriptBlock(
                        "$ScPsSlvPipelineObject | Foreach-Object { $_.PSStandardMembers.DefaultDisplayProperty } | Select-Object -First 1");
                var propDefault = InvokeCommand.InvokeScript(SessionState, propScript).FirstOrDefault();
                if (propDefault == null)
                {
                    hasCustomObjects = true;
                    // May be PSCustomObject
                    propScript =
                        InvokeCommand.NewScriptBlock(
                            "$ScPsSlvPipelineObject | Foreach-Object { $_.PSObject.Properties.Name } | Select-Object -First 1");
                    propDefault = InvokeCommand.InvokeScript(SessionState, propScript).FirstOrDefault();
                }
                if (propDefault != null)
                {
                    propScript = InvokeCommand.NewScriptBlock(hasCustomObjects 
                        ? "$ScPsSlvPipelineObject | Foreach-Object { $_.PSObject.Properties.Name }" 
                        : "$ScPsSlvPipelineObject | Foreach-Object { $_.PSStandardMembers.DefaultDisplayPropertySet.ReferencedPropertyNames }");

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
                var formatProperty = SessionState.PSVariable.Get("ScPsSlvProperties")?.Value;
                var script = (formatProperty is string)
                    ? "$ScPsSlvPipelineObject | Select-Object -Property " + formatProperty
                    : "$ScPsSlvPipelineObject | Select-Object -Property $ScPsSlvProperties";

                var scriptBlock = InvokeCommand.NewScriptBlock(script);
                var result = InvokeCommand.InvokeScript(SessionState, scriptBlock);

                if (result.Any())
                {
                    var varValue = Data.BaseObject();
                    if (varValue is PSCustomObject)
                    {
                        varValue = Data;
                    }

                    var slvDataObject = new DataObject
                    {
                        Original = varValue,
                        Id = CumulativeData.Count
                    };

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