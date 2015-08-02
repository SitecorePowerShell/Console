using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Validation;
using Sitecore.ContentSearch;

namespace Cognifide.PowerShell.Commandlets
{
    public class BaseIndexCommand : BaseCommand
    {
        public static readonly string[] Indexes = ContentSearchManager.Indexes.Select(i => i.Name).ToArray();

        [AutocompleteSet("Indexes")]
        [Parameter(ValueFromPipelineByPropertyName = true, ValueFromPipeline = true, Position = 0, ParameterSetName = "Name")]
        public string Name { get; set; }
    }
}