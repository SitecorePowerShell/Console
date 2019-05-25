using System.Globalization;
using System.Management.Automation;

namespace Spe.Commandlets.Interactive
{
    public class BaseFormCommand : BaseShellCommand
    {
        private int height;
        private int width;

        public BaseFormCommand()
        {
            width = 800;
            height = 600;
        }

        [Parameter]
        public virtual string Title { get; set; }

        [Parameter]
        public virtual string Icon { get; set; }

        [Parameter]
        public virtual int Width
        {
            get => width;
            set => width = value;
        }

        [Parameter]
        public virtual int Height
        {
            get => height;
            set => height = value;
        }

        protected string WidthString => Width.ToString(CultureInfo.InvariantCulture);

        protected string HeightString => Height.ToString(CultureInfo.InvariantCulture);

        protected void AssertDefaultSize(int defaultWidth, int defaultHeight)
        {
            if (!IsParameterSpecified(nameof(Width)))
            {
                Width = defaultWidth;
            }
            if (!IsParameterSpecified(nameof(Height)))
            {
                Height = defaultHeight;
            }
        }
    }
}