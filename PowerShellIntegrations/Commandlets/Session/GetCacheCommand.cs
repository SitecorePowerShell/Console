using System;
using System.Management.Automation;
using Sitecore.Caching;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Session
{
    [Cmdlet("Get", "Cache")]
    [OutputType(new[] {typeof (Cache)})]
    public class GetCacheCommand : BaseCommand
    {
        [ValidatePattern("[\\*\\?\\[\\]\\-0-9a-zA-Z_]+")]
        [Parameter(Position = 0)]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            if (!String.IsNullOrEmpty(Name))
            {
                WildcardWrite(Name, CacheManager.GetAllCaches(), p => p.Name);
            }
            else
            {
                WriteObject(CacheManager.GetAllCaches(), true);
            }
        }
    }
}