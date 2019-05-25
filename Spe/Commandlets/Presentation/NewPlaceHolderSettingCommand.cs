using System.Management.Automation;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Spe.Core.Extensions;

namespace Spe.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.New, "PlaceholderSetting")]
    [OutputType(typeof (PlaceholderDefinition),
        ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"})]
    public class NewPlaceholderSettingCommand : BaseItemCommand
    {       
        [Parameter]
        public string Key { get; set; }

        protected override void ProcessItem(Item item)
        {
            var phs = new PlaceholderDefinition
            {                
                MetaDataItemId = item.Paths.FullPath,
                Key = Key,
                UniqueId = ID.NewID.ToString()
            };

            var psobj = ItemShellExtensions.WrapInItemOwner(SessionState, item, phs);
            WriteObject(psobj);
        }
    }
}