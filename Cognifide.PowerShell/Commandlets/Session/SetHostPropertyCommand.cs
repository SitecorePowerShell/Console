using System;
using System.Management.Automation;
using System.Management.Automation.Host;
using Cognifide.PowerShell.Core.Settings;

namespace Cognifide.PowerShell.Commandlets.Session
{
    [Cmdlet(VerbsCommon.Set, "HostProperty")]
    public class SetHostPropertyCommand : BaseCommand
    {
        [Parameter]
        public ConsoleColor ForegroundColor { get; set; }

        [Parameter]
        public ConsoleColor BackgroundColor { get; set; }

        [Parameter]
        public int HostWidth { get; set; }

        [Parameter]
        public SwitchParameter Persist { get; set; }

        protected override void ProcessRecord()
        {
            var settings = SessionState.PSVariable.Get("HostSettings").Value as ApplicationSettings;

            if (settings == null)
            {
                return;
            }

            if (MyInvocation.BoundParameters.ContainsKey("ForegroundColor"))
            {
                Host.UI.RawUI.ForegroundColor = ForegroundColor;
                if (Persist)
                {
                    settings.ForegroundColor = ForegroundColor;
                }
            }

            if (MyInvocation.BoundParameters.ContainsKey("BackgroundColor"))
            {
                Host.UI.RawUI.BackgroundColor = BackgroundColor;
                if (Persist)
                {
                    settings.BackgroundColor = BackgroundColor;
                }
            }

            if (MyInvocation.BoundParameters.ContainsKey("HostWidth"))
            {
                Host.UI.RawUI.BufferSize = new Size(HostWidth, Int32.MaxValue);
                if (Persist)
                {
                    settings.HostWidth = HostWidth;
                }
            }

            if (Persist)
            {
                settings.Save();
            }
        }
    }
}