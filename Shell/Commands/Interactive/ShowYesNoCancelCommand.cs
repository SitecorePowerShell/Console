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
    [Cmdlet(VerbsCommon.Show, "YesNoCancel", SupportsShouldProcess = true, DefaultParameterSetName = "Name")]
    public class ShowYesNoCancelCommand : BaseFormCommand
    {
        protected override void ProcessRecord()
        {
            LogErrors(() =>
                {
                    string yesnoresult = JobContext.ShowModalDialog(Title, "YesNoCancel", WidthString, HeightString);
                    WriteObject(yesnoresult);
                });
        }
    }
}