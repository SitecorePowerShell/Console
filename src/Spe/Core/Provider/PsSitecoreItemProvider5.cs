using System.Management.Automation;
using System.Management.Automation.Provider;
using Sitecore.Data.Items;

namespace Spe.Core.Provider
{
    [CmdletProvider("PsSitecoreItemProvider5",
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