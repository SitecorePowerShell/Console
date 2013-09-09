using System.Globalization;
using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    public class BaseFormCommand : BaseShellCommand
    {
        [Parameter]
        public string Title { get; set; }

        [Parameter]
        public int Width { get; set; }

        [Parameter]
        public int Height { get; set; }

        protected string WidthString { get { return Width.ToString(CultureInfo.InvariantCulture); } }
        protected string HeightString { get { return Height.ToString(CultureInfo.InvariantCulture); } }
    }
}