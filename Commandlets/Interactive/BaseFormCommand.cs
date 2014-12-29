using System.Globalization;
using System.Management.Automation;

namespace Cognifide.PowerShell.Commandlets.Interactive
{
    public class BaseFormCommand : BaseShellCommand
    {
        [Parameter]
        public virtual string Title { get; set; }

        [Parameter]
        public virtual int Width { get; set; }

        [Parameter]
        public virtual int Height { get; set; }

        public BaseFormCommand()
        {
            Width = 800;
            Height = 600;
        }
        protected string WidthString
        {
            get { return Width.ToString(CultureInfo.InvariantCulture); }
        }

        protected string HeightString
        {
            get { return Height.ToString(CultureInfo.InvariantCulture); }
        }
    }
}