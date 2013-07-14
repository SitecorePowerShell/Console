using System;
using System.Collections;
using System.IO;
using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.IO;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Resources.Media;
using Sitecore.Shell;
using Sitecore.Shell.Framework;
using Sitecore.Text;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    [Cmdlet(VerbsCommon.Close, "Window", SupportsShouldProcess = true)]
    public class CloseDialogCommand : BaseFormCommand
    {
        protected override void ProcessRecord()
        {
            JobContext.MessageQueue.PutMessage(new CloseWindowMessage());
        }
    }
}