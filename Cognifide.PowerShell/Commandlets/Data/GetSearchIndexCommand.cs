using System.Management.Automation;
using Sitecore.ContentSearch;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet(VerbsCommon.Get, "SearchIndex", DefaultParameterSetName = "Name")]
    [OutputType(typeof (ISearchIndex))]
    public class GetSearchIndexCommand : BaseCommand
    {
        [ValidatePattern("[\\*\\?\\[\\]\\-0-9a-zA-Z_]+")]
        [Parameter(ValueFromPipeline = true, Position = 0, ParameterSetName = "Name")]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            if (Name != null)
            {
                WildcardWrite(Name, ContentSearchManager.Indexes, index => index.Name);
            }
            else
            {
                WriteObject(ContentSearchManager.Indexes, true);
            }
        }
    }
}