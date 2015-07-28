using System.Linq;
using System.Management.Automation;
using Sitecore.ContentSearch;

namespace Cognifide.PowerShell.Commandlets
{
    public class BaseIndexCommand : BaseCommand
    {
        private readonly string[] indexes = ContentSearchManager.Indexes.Select(i => i.Name).ToArray();

        [ValidateSet("*")]
        [Parameter(ValueFromPipeline = true, Position = 0, ParameterSetName = "Name")]
        public string Name { get; set; }

        public override object GetDynamicParameters()
        {
            if (!_reentrancyLock.WaitOne(0))
            {
                _reentrancyLock.Set();

                SetValidationSetValues("Name", indexes);

                _reentrancyLock.Reset();
            }

            return base.GetDynamicParameters();
        }
    }
}