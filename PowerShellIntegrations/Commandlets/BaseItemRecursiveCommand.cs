using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Data;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets
{
    public abstract class BaseItemRecursiveCommand : BaseItemCommand
    {
        [Parameter]
        public SwitchParameter Recurse { get; set; }

        protected override void ProcessRecord()
        {
            Item item = FindItemFromParameters(Item, Path, Id, null, Database);
            ProcessItemLanguages(item);
        }

        private void ProcessItemLanguages(Item item)
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
            if (Recurse)
            {
                foreach (Item child in item.Children)
                {
                    ProcessItem(child);
                }
            }
        }
    }
}