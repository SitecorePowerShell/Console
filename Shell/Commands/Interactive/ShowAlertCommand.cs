using System;
using System.Collections;
using System.Globalization;
using System.Management.Automation;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Web;

namespace Cognifide.PowerShell.Shell.Commands.Interactive
{
    [Cmdlet(VerbsCommon.Show, "Alert", SupportsShouldProcess = true, DefaultParameterSetName = "Name")]
    public class ShowAlertCommand : BaseShellCommand
    {
        [Parameter(ValueFromPipeline = true, Position = 0, Mandatory = true)]
        public string Title { get; set; }

        protected override void ProcessRecord()
        {
            LogErrors(() =>
                {
                    JobContext.Alert(Title);
                });
        }
    }
}