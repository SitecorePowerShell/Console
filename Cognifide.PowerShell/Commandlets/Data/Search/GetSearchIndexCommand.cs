using System.Management.Automation;
using Sitecore.ContentSearch;

namespace Cognifide.PowerShell.Commandlets.Data.Search
{
    [Cmdlet(VerbsCommon.Get, "SearchIndex", DefaultParameterSetName = "Name")]
    [OutputType(typeof (ISearchIndex))]
    public class GetSearchIndexCommand : BaseIndexCommand
    {
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