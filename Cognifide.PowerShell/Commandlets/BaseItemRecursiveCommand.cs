using System.Management.Automation;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets
{
    public abstract class BaseItemRecursiveCommand : BaseItemCommand
    {
        [Parameter]
        public SwitchParameter Recurse { get; set; }

        protected override void ProcessRecord()
        {
            var item = FindItemFromParameters(Item, Path, Id, null, Database);
            ProcessItemLanguages(item);
        }

        protected override void ProcessItemLanguages(Item item)
        {
            if (Language == null)
            {
                ProcessItem(item);
                ProcessChildren(item);
            }
            else
            {
                foreach (var langItem in LatestVersionInFilteredLanguages(item))
                {
                    ProcessItem(langItem);
                    ProcessChildren(item);
                }
            }
        }

        //override this method if your commandlet handles recursion itself.
        protected virtual void ProcessChildren(Item item)
        {
            if (!Recurse) return;

            foreach (Item child in item.Children)
            {
                ProcessItem(child);
            }
        }
    }
}