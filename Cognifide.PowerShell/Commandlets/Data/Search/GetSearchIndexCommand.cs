using System;
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
                WriteVerbose($"Getting index with name {Name}.");
                WildcardWrite(Name, ContentSearchManager.Indexes, index => index.Name);
            }
            else
            {
                WriteVerbose("Getting all indexes.");
                WriteObject(ContentSearchManager.Indexes, true);
            }
        }
    }
}