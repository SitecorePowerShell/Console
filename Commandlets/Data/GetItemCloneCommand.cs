using System.Linq;
using System.Management.Automation;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet("Get", "ItemClone")]
    [OutputType(typeof (Item), ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"})]
    public class GetItemCloneCommand : BaseLanguageAgnosticItemCommand
    {
        protected override void ProcessItem(Item item)
        {
            item.GetClones().ToList().ForEach(WriteItem);
        }
    }
}