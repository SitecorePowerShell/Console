using System;
using System.Management.Automation;
using System.Management.Automation.Host;
using Spe.Core.Settings;

namespace Spe.Commands.Session
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
        public int HostHeight { get; set; }

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

            if (MyInvocation.BoundParameters.ContainsKey(nameof(HostWidth)))
            {
                Host.UI.RawUI.BufferSize = new Size(HostWidth, Host.UI.RawUI.BufferSize.Height);
                if (Persist)
                {
                    settings.HostWidth = HostWidth;
                }
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(HostHeight)))
            {
                Host.UI.RawUI.BufferSize = new Size(Host.UI.RawUI.BufferSize.Width, HostHeight);
                if (Persist)
                {
                    settings.HostHeight = HostHeight;
                }
            }

            if (Persist)
            {
                settings.Save();
            }
        }
    }
}