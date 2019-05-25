using System.Linq;
using System.Management.Automation;
using Sitecore.Data.Items;

namespace Spe.Commands.Data.Clones
{
    [Cmdlet(VerbsCommon.Get, "ItemClone")]
    [OutputType(typeof (Item), ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"})]
    public class GetItemCloneCommand : BaseLanguageAgnosticItemCommand
    {
        protected override void ProcessItem(Item item)
        {
            item.GetClones().ToList().ForEach(WriteItem);
        }
    }
}