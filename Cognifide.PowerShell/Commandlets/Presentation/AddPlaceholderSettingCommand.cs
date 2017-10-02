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
        public string UniqueId { get; set; }

        [Parameter]
        public string Key { get; set; }


        protected override void ProcessLayout(Item item, LayoutDefinition layout, DeviceDefinition device)
        {
            if (!ShouldProcess(item.GetProviderPath(), "Add PlaceholderSetting " + Instance.MetaDataItemId))
            {
                return;
            }
            var placeholder = new PlaceholderDefinition
            {                
                Key = Key ?? Instance.Key,
                MetaDataItemId= MetaDataItemId ??Instance.MetaDataItemId,
                UniqueId = UniqueId?? Instance.UniqueId
            };
            if(!DoesPlaceHolderSettingAlreadyExists(device,placeholder))
                device.AddPlaceholder(placeholder);
            
                        

            item.Edit(p =>
            {
                var outputXml = layout.ToXml();
                LayoutField.SetFieldValue(item.Fields[LayoutFieldId], outputXml);
            });
        }

        private bool DoesPlaceHolderSettingAlreadyExists(DeviceDefinition device, PlaceholderDefinition ph)
        {
            bool result = false;
            for (int i = 0; i < device.Placeholders.Count; i++)
            {
                PlaceholderDefinition p = (PlaceholderDefinition)device.Placeholders[i];
                if(p.Key.Equals(ph.Key) && p.MetaDataItemId.Equals(ph.MetaDataItemId))
                {
                    result = true;
                    break;
                }
            }
            return result;
        }
    }
}