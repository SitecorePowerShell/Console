using System;
using System.Collections.Generic;
using System.Data.Common.CommandTrees;
using System.Linq;
using System.Management.Automation;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using Sitecore.Security.Accounts;
using Sitecore.Security.Authentication;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Governance
{
    [Cmdlet("Get", "ItemLock")]
    [OutputType(new[] { typeof(User) }, ParameterSetName = new[] { "Item from Pipeline", "Item from Path", "Item from ID" })]
    public class GetItemLockCommand : GovernanceUserBaseCommand
    {       

        protected override void ProcessRecord()
        {
            Item item = GetProcessedRecord();
            string owner = item.Locking.GetOwner();
            if (!string.IsNullOrEmpty(owner))
            {
                WriteObject(owner);
            }
        }

    }
}