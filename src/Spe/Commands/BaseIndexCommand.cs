using System.Linq;
using System.Management.Automation;
using Sitecore.ContentSearch;
using Spe.Core.Validation;

namespace Spe.Commands
{
    public class BaseIndexCommand : BaseCommand
    {
        public static readonly string[] Indexes = ContentSearchManager.Indexes.Select(i => i.Name).ToArray();

        [AutocompleteSet(nameof(Indexes))]
        [Parameter(ParameterSetName = "Name")]
        [Parameter(ParameterSetName = "Item")]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }
    }
}