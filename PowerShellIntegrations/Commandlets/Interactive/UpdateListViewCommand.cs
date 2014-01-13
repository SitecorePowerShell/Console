using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Web.Script.Services;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages;
using Cognifide.PowerShell.PowerShellIntegrations.Host;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    [Cmdlet("Update", "ListView")]
    [OutputType(new[] {typeof (string)})]
    public class UpdateListViewCommand : BaseListViewCommand
    {

        protected override void EndProcessing()
        {
            LogErrors(() => SessionState.PSVariable.Set("allData",cumulativeData));
            base.EndProcessing();
        }
    }
}