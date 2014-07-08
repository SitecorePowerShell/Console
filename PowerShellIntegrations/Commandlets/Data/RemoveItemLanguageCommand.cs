using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using Sitecore.Publishing;
using Sitecore.Publishing.Pipelines.PublishItem;
using Sitecore.Shell.Framework.Commands;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Data
{
    [Cmdlet(VerbsCommon.Remove, "ItemLanguage")]
    public class RemoveItemLanguageCommand : BaseItemCommand
    {
        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Alias("Languages")]
        [Parameter(ParameterSetName = "Item from Path", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from ID", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from Pipeline", Mandatory = true)]
        public override string[] Language { get; set; }

        protected override void ProcessItemLanguages(Item item)
        {
            foreach (Item langItem in item.Versions.GetVersions(true))
            {
                if (LanguageWildcardPatterns.Any(wildcard => wildcard.IsMatch(langItem.Language.Name)))
                {
                    langItem.Versions.RemoveAll(false);
                }
            }
            if (Recurse)
            {
                foreach (Item child in item.Children)
                {
                    ProcessItemLanguages(child);
                }
            }
        }

        protected override void ProcessItem(Item item)
        {
            // this function is not used due to override on ProcessItemLanguages
        }

    }

}