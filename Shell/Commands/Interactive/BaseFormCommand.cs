using System;
using System.Globalization;
using System.Management.Automation;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Jobs.AsyncUI;

namespace Cognifide.PowerShell.Shell.Commands.Interactive
{
    public class BaseFormCommand : BaseShellCommand
    {
        [Parameter(ValueFromPipeline = true, Position = 0)]
        public string Title { get; set; }

        [Parameter(Position = 1)]
        public int Width { get; set; }

        [Parameter(Position = 2)]
        public int Height { get; set; }

        protected string WidthString { get { return Width.ToString(CultureInfo.InvariantCulture); } }
        protected string HeightString { get { return Height.ToString(CultureInfo.InvariantCulture); } }
    }
}