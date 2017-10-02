using System.Collections;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Sitecore.Text;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Add, "PlaceHolderSetting", SupportsShouldProcess = true)]
    public class AddPlaceHolderSettingCommand : BaseLayoutPerDeviceCommand
    {
        private int index = -1;

        [Parameter(Mandatory = true)]
        [Alias("PlaceHolderSetting")]
        public PlaceholderDefinition Instance { get; set; }

        [Parameter]
        public string MetaDataItemId { get; set; }

        [Parameter]
        public int Index
        {
            get { return index; }
            set { index = value; }
        }

        protected override void ProcessLayout(Item item, LayoutDefinition layout, DeviceDefinition device)
        {
            if (!ShouldProcess(item.GetProviderPath(), "Add PlaceholderSetting " + Instance.MetaDataItemId))
            {
                return;
            }
            var placeholder = new PlaceholderDefinition
            {
                DynamicProperties = Instance.DynamicProperties,
                Key = Instance.Key,
                MetaDataItemId= MetaDataItemId ??Instance.MetaDataItemId,
                UniqueId = Instance.UniqueId
            };

            

            //todo: add support for conditions
            //renderingDefinition.Conditions
            //todo: add support for multivariate tests
            //rendering.MultiVariateTest

            if (Index == -1)
            {
                device.AddPlaceholder(placeholder);
            }            

            item.Edit(p =>
            {
                var outputXml = layout.ToXml();
                LayoutField.SetFieldValue(item.Fields[LayoutFieldId], outputXml);
            });
        }
    }
}