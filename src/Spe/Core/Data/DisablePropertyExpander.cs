using Sitecore.Common;

namespace Spe.Core.Data
{
    public class DisablePropertyExpander : Switcher<bool, DisablePropertyExpander>
    {
        public DisablePropertyExpander() : base(true) { }
    }
}