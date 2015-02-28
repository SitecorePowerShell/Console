using System;
using System.Management.Automation;
using Sitecore.Configuration;
using Sitecore.Search;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet(VerbsCommon.Get, "SearchIndex")]
    [OutputType(typeof (Index))]
    public class GetSearchIndexCommand : BaseCommand
    {
        private static SearchConfiguration _configuration;

        [ValidatePattern("[\\*\\?\\[\\]\\-0-9a-zA-Z_]+")]
        [Parameter(Position = 1)]
        public string Name { get; set; }

        private static SearchConfiguration SearchConfig
        {
            get
            {
                return _configuration ??
                       (_configuration = Factory.CreateObject("search/configuration", true) as SearchConfiguration);
            }
        }

        protected override void ProcessRecord()
        {
            if (String.IsNullOrEmpty(Name))
            {
                foreach (var index in SearchConfig.Indexes.Keys)
                {
                    WriteObject(SearchManager.GetIndex(index), true);
                }
            }
            else
            {
                foreach (var index in WildcardFilter(Name, SearchConfig.Indexes.Keys, name => name))
                {
                    WriteObject(SearchManager.GetIndex(index), true);
                }
            }
        }
    }
}