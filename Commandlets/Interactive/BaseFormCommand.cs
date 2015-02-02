using System.Globalization;
using System.Management.Automation;

namespace Cognifide.PowerShell.Commandlets.Interactive
{
    public class BaseFormCommand : BaseShellCommand
    {
        private int width;
        private int height;

        [Parameter]
        public virtual string Title { get; set; }

        [Parameter]
        public virtual int Width
        {
            get { return width; }
            set { width = value; }
        }

        [Parameter]
        public virtual int Height
        {
            get { return height; }
            set { height = value; }
        }

        public BaseFormCommand()
        {
            width = 800;
            height = 600;
        }
        protected string WidthString
        {
            get { return Width.ToString(CultureInfo.InvariantCulture); }
        }

        protected string HeightString
        {
            get { return Height.ToString(CultureInfo.InvariantCulture); }
        }

        protected void AssertDefaultSize(int defaultWidth, int defaultHeight)
        {
            if (!IsParameterSpecified("Width"))
            {
                Width = defaultWidth;
            }
            if (!IsParameterSpecified("Height"))
            {
                Height = defaultHeight;
            }

            
        }
    }
}