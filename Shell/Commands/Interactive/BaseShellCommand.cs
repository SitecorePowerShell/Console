using System;
using System.Globalization;
using System.Management.Automation;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Jobs.AsyncUI;

namespace Cognifide.PowerShell.Shell.Commands.Interactive
{
    public class BaseShellCommand : BaseCommand
    {
        protected override void BeginProcessing()
        {
            LogErrors(() =>
                {
                    Context.Site = Factory.GetSite(Context.Job.Options.SiteName);
                });
        }
    }
}