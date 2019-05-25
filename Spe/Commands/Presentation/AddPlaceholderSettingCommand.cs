using System;
using System.Data;
using System.Linq;
using System.Management.Automation;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Spe.Core.Extensions;
using Spe.Core.Utility;

namespace Spe.Commands.Presentation
{
    [Cmdlet(VerbsCommon.Add, "PlaceholderSetting", SupportsShouldProcess = true)]
    public class AddPlaceholderSettingCommand : BaseLayoutPerDeviceCommand
    {
        [Parameter(Mandatory = true)]
        [Alias("PlaceholderSetting")]
        public PlaceholderDefinition Instance { get; set; }

        [Parameter]
        public string MetaDataItemId { get; set; }

        [Parameter]
        public string Key { get; set; }


        protected override void ProcessLayout(Item item, LayoutDefinition layout, DeviceDefinition device)
        {
            var key = Key ?? Instance.Key;

            if (string.IsNullOrWhiteSpace(key))
            {
                WriteError(typeof(ObjectNotFoundException), "A placeholder setting requires a key provided here or in the instance.",
                    ErrorIds.ValueNotFound, ErrorCategory.ObjectNotFound, key);

                return;
            }

            var metadataItemId = MetaDataItemId ?? Instance.MetaDataItemId;

            if (string.IsNullOrWhiteSpace(metadataItemId))
            {
                WriteError(typeof(ObjectNotFoundException), "A placeholder setting requires a MetaDataItemId provided here or in the instance.",
                    ErrorIds.ValueNotFound, ErrorCategory.ObjectNotFound, metadataItemId);

                return;
            }

            if (!ShouldProcess(item.GetProviderPath(), $"Add placeholder setting '{metadataItemId}'"))
            {
                return;
            }

            var placeholder = new PlaceholderDefinition
            {                
                Key = key,
                MetaDataItemId = metadataItemId,
                UniqueId = ID.NewID.ToString()
            };

            if (!PlaceholderSettingExists(device, placeholder))
            {
                device.AddPlaceholder(placeholder);

                item.Edit(p =>
                {
                    var outputXml = layout.ToXml();
                    LayoutField.SetFieldValue(item.Fields[LayoutFieldId], outputXml);
                });
            }
            else
            {
                WriteError(typeof(DuplicateNameException), $"A placeholder setting with key '{key}' and metadata '{metadataItemId}' already exists.",
                    ErrorIds.PlaceholderSettingAlreadyExists, ErrorCategory.InvalidArgument, placeholder);
                return;
            }
        }

        private bool PlaceholderSettingExists(DeviceDefinition device, PlaceholderDefinition ph)
        {
            return device.Placeholders
                         .Cast<PlaceholderDefinition>()
                         .Any(p => p.Key.Equals(ph.Key) && p.MetaDataItemId.Equals(ph.MetaDataItemId, StringComparison.OrdinalIgnoreCase));
        }
    }
}