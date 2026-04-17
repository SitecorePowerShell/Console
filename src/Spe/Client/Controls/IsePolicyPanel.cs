using Sitecore.Configuration;
using Sitecore.Data.Items;
using Spe.Core.Settings.Authorization;

namespace Spe.Client.Controls
{
    public class IsePolicyPanel : IseContextPanelBase
    {
        protected override Item Button1 => Factory.GetDatabase("core").GetItem("{8BB187CF-3D5B-46B8-AEB2-A6BDE538629C}");
        protected override Item Button2 => null;

        protected override string Label1 =>
            RemotingPolicyManager.ResolvePolicyItem(CommandContext.Parameters["currentPolicy"])?.DisplayName
            ?? "[None]";

        // Empty string tells the ribbon renderer to fall back to Button1's Icon
        // field (the stock brickwall), which is the right visual for "no policy".
        protected override string Icon1 =>
            RemotingPolicyManager.ResolvePolicyItem(CommandContext.Parameters["currentPolicy"])?.Appearance?.Icon
            ?? string.Empty;

        protected override string Label2 => null;
        protected override string Icon2 => null;
    }
}
