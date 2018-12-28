using System.Collections;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Sitecore.Text;
using System.Xml;
using System;
using Sitecore.Data;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.New, "PlaceholderSetting")]
    [OutputType(typeof (PlaceholderDefinition),
        ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"})]
    public class NewPlaceholderSettingCommand : BaseItemCommand
    {       
        [Parameter]
        public string Key { get; set; }

        [Parameter]
        public ID UniqueId { get; set; }

        protected override void ProcessItem(Item item)
        {
            var phs = new PlaceholderDefinition
            {                
                MetaDataItemId = item.Paths.FullPath,
                Key = Key,
                UniqueId = (UniqueId ?? ID.NewID).ToString()
            };

            var psobj = ItemShellExtensions.WrapInItemOwner(SessionState, item, phs);
            WriteObject(psobj);
        }
    }
}