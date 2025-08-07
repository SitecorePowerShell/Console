using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Sitecore.Text;
using Spe.Core.Extensions;
using Spe.Core.Utility;

namespace Spe.Commands.Presentation
{
    [Cmdlet(VerbsCommon.Add, "Rendering", SupportsShouldProcess = true)]
    public class AddRenderingCommand : BaseLayoutPerDeviceCommand
    {
        [Parameter(Mandatory = true)]
        [Alias("Rendering")]
        public RenderingDefinition Instance { get; set; }

        [Parameter]
        public Hashtable Parameter { get; set; }

        [Parameter(Mandatory = true)]
        public string PlaceHolder { get; set; }

        [Parameter]
        public string DataSource { get; set; }

        [Parameter]
        public int Index { get; set; } = -1;

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessLayout(Item item, LayoutDefinition layout, DeviceDefinition device)
        {
            if (!ShouldProcess(item.GetProviderPath(), "Add rendering " + Instance.ItemID))
            {
                return;
            }
            var rendering = new RenderingDefinition
            {
                ItemID = Instance.ItemID,
                Placeholder = PlaceHolder ?? Instance.Placeholder,
                Datasource = DataSource ?? Instance.Datasource,
                Cachable = Instance.Cachable,
                ClearOnIndexUpdate = Instance.ClearOnIndexUpdate,
                VaryByData = Instance.VaryByData,
                VaryByDevice = Instance.VaryByDevice,
                VaryByLogin = Instance.VaryByLogin,
                VaryByParameters = Instance.VaryByParameters,
                VaryByQueryString = Instance.VaryByQueryString,
                VaryByUser = Instance.VaryByUser
            };

            if (Parameter != null && Parameter.Keys.Count > 0)
            {
                var parameters = new UrlString(rendering.Parameters ?? string.Empty);
                var renderingItem = item.Database.GetItem(Instance.ItemID);
                if(renderingItem == null)
                {
                    WriteError(new ItemNotFoundException($"The rendering with Id {Instance.ItemID} could not be found in context database."), ErrorIds.ItemNotFound, ErrorCategory.ObjectNotFound, this);
                    return;
                }

                var standardValuesItem = RenderingItem.GetStandardValuesItemFromParametersTemplate(renderingItem);
                var excludedFields = new List<string> { "additional parameters", "placeholder", "data source", "caching", "personalization", "test" };
                foreach (Field standardValueField in standardValuesItem.Fields)
                {
                    var fieldName = standardValueField.Name;
                    var lowerInvariant = fieldName.ToLowerInvariant();
                    if (excludedFields.Contains(lowerInvariant)) continue;
                    if (!RenderingItem.IsRenderingParameterField(standardValueField)) continue;

                    var fieldValue = standardValueField.Value ?? string.Empty;
                    if (parameters.Parameters.AllKeys.Contains(fieldName))
                    {
                        parameters.Parameters[fieldName] = fieldValue;
                    } 
                    else
                    {
                        parameters.Add(fieldName, fieldValue);
                    }

                }
                foreach (string name in Parameter.Keys)
                {
                    if (parameters.Parameters.AllKeys.Contains(name))
                    {
                        parameters.Parameters[name] = Parameter[name].ToString();
                    }
                    else
                    {
                        parameters.Add(name, Parameter[name].ToString());
                    }
                }
                rendering.Parameters = parameters.ToString();
            }

            //todo: add support for conditions
            //renderingDefinition.Conditions
            //todo: add support for multivariate tests
            //rendering.MultiVariateTest

            if (Index > -1)
            {
                device.Insert(Index, rendering);
            }
            else
            {
                device.AddRendering(rendering);
            }

            item.Edit(p =>
            {
                var outputXml = layout.ToXml();
                LayoutField.SetFieldValue(item.Fields[LayoutFieldId], outputXml);
            });

            if (PassThru)
            {
                WriteObject(rendering);
            }
        }
    }
}