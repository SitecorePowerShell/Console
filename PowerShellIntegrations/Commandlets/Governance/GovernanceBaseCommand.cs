using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Governance
{
    public class GovernanceBaseCommand : BaseCommand
    {       

        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Item from Pipeline")]
        public Item Item { get; set; }

        [Parameter(ParameterSetName = "Item from Path")]
        [Alias("FullName", "FileName")]
        public string Path { get; set; }

        [Parameter(ParameterSetName = "Item from ID")]
        public string Id { get; set; }

        [Parameter(ParameterSetName = "Item from Path")]
        [Parameter(ParameterSetName = "Item from ID")]
        public Language Language { get; set; }

        [Parameter]
        public SwitchParameter FailSilently { get; set; }
        
        [Parameter]
        public SwitchParameter Recurse { get; set; }

        protected Item GetProcessedRecord()
        {
            return FindItemFromParameters(Item, Path, Id, Language);
        }

    }
}