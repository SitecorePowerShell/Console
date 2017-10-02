using System.Collections;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Sitecore.Text;
using System.Xml;
using System;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.New, "PlaceHolderSetting")]
    [OutputType(typeof (PlaceholderDefinition),
        ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"})]
    public class NewPlaceHolderSettingCommand : BaseItemCommand
    {       
        [Parameter]
        public string Key { get; set; }

        [Parameter]
        public string UniqueId { get; set; }

        protected override void ProcessItem(Item item)
        {
            var phs = new PlaceholderDefinition
            {                
                MetaDataItemId = item.ID.ToString(),
                Key = Key,
                UniqueId = UniqueId ?? Guid.NewGuid().ToString()
            };

            var psobj = ItemShellExtensions.WrapInItemOwner(SessionState, item, phs);
            WriteObject(psobj);
        }
    }
}