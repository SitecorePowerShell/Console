using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Management.Automation.Runspaces;
using System.Web;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Core.Provider
{
    [CmdletProvider("PsSitecoreItemProvider",
       ProviderCapabilities.Filter | ProviderCapabilities.ShouldProcess | ProviderCapabilities.ExpandWildcards)]
    [OutputType(typeof(Item), ProviderCmdlet = "Get-ChildItem")]
    [OutputType(typeof(Item), ProviderCmdlet = "Get-Item")]
    [OutputType(typeof(Item), ProviderCmdlet = "New-Item")]
    [OutputType(typeof(Item), ProviderCmdlet = "Copy-Item")]
    public class PsSitecoreItemProvider5 : PsSitecoreItemProvider
    {
        protected override void GetChildItems(string path, bool recurse, uint depth)
        {
            GetChildItemsWithDepth(path, recurse, depth);
        }

        protected override bool AppendDepthParameterIfNotNativelySupported(bool paramAdded, ref RuntimeDefinedParameterDictionary dic)
        {
            return paramAdded;
        }

    }
}