using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Spe.Core.Validation;

namespace Spe.Commands.Presentation
{
    public abstract class BasePlaceholderSettingCommand : BaseLayoutPerDeviceCommand
    {
        private int index = -1;

        [Parameter(Mandatory = true, ParameterSetName = "PlaceholderSetting by filter, Item from Pipeline",
            ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Parameter(Mandatory = true, ParameterSetName = "PlaceholderSetting by instance, Item from Pipeline",
            ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Parameter(Mandatory = true, ParameterSetName = "PlaceholderSetting by unique ID, Item from Pipeline",
            ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public override Item Item { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "PlaceholderSetting by filter, Item from Path")]
        [Parameter(Mandatory = true, ParameterSetName = "PlaceholderSetting by instance, Item from Path")]
        [Parameter(Mandatory = true, ParameterSetName = "PlaceholderSetting by unique ID, Item from Path")]
        [Alias("FullName", "FileName")]
        public override string Path { get; set; }

        [Parameter(ParameterSetName = "PlaceholderSetting by filter, Item from ID")]
        [Parameter(ParameterSetName = "PlaceholderSetting by instance, Item from ID")]
        [Parameter(ParameterSetName = "PlaceholderSetting by unique ID, Item from ID")]
        public override string Id { get; set; }

        [AutocompleteSet(nameof(Databases))]
        [Parameter(ParameterSetName = "PlaceholderSetting by filter, Item from ID")]
        [Parameter(ParameterSetName = "PlaceholderSetting by instance, Item from ID")]
        [Parameter(ParameterSetName = "PlaceholderSetting by unique ID, Item from ID")]
        public override string Database { get; set; }

        [Parameter(ParameterSetName = "PlaceholderSetting by filter, Item from Pipeline")]
        [Parameter(ParameterSetName = "PlaceholderSetting by filter, Item from Path")]
        [Parameter(ParameterSetName = "PlaceholderSetting by filter, Item from ID")]
        public string Key { get; set; }

        [Parameter(ParameterSetName = "PlaceholderSetting by filter, Item from Pipeline")]
        [Parameter(ParameterSetName = "PlaceholderSetting by filter, Item from Path")]
        [Parameter(ParameterSetName = "PlaceholderSetting by filter, Item from ID")]
        public Item PlaceholderSetting { get; set; }

        [Parameter(ParameterSetName = "PlaceholderSetting by filter, Item from Pipeline")]
        [Parameter(ParameterSetName = "PlaceholderSetting by filter, Item from Path")]
        [Parameter(ParameterSetName = "PlaceholderSetting by filter, Item from ID")]
        public int Index
        {
            get { return index; }
            set { index = value; }
        }

        [Parameter(Mandatory = true, ParameterSetName = "PlaceholderSetting by instance, Item from Pipeline")]
        [Parameter(Mandatory = true, ParameterSetName = "PlaceholderSetting by instance, Item from Path")]
        [Parameter(Mandatory = true, ParameterSetName = "PlaceholderSetting by instance, Item from ID")]
        public PlaceholderDefinition Instance { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "PlaceholderSetting by unique ID, Item from Pipeline")]
        [Parameter(Mandatory = true, ParameterSetName = "PlaceholderSetting by unique ID, Item from Path")]
        [Parameter(Mandatory = true, ParameterSetName = "PlaceholderSetting by unique ID, Item from ID")]
        public string UniqueId { get; set; }

        protected override void ProcessLayout(Item item, LayoutDefinition layout, DeviceDefinition device)
        {
            if (device?.Renderings == null)
            {
                return;
            }

            var placeholders = device.Placeholders.Cast<PlaceholderDefinition>();

            if (Instance != null)
            {
                placeholders = new[] {Instance};
            }
            else if (!string.IsNullOrEmpty(UniqueId))
            {
                placeholders =
                    placeholders.Where(r => string.Equals(r.UniqueId, UniqueId, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                if (PlaceholderSetting != null)
                {
                    placeholders = placeholders.Where(r => r.MetaDataItemId == PlaceholderSetting.Paths.FullPath);
                }

                if (!string.IsNullOrEmpty(Key))
                {
                    placeholders = WildcardFilter(Key, placeholders, r => r.Key);
                }

                if (Index > -1)
                {
                    placeholders = placeholders.Skip(index).Take(1);
                }
            }

            ProcessPlaceholderSettings(item, layout, device, placeholders.ToList());
        }

        protected abstract void ProcessPlaceholderSettings(Item item, LayoutDefinition layout, DeviceDefinition device,
            List<PlaceholderDefinition> placeholders);
    }
}