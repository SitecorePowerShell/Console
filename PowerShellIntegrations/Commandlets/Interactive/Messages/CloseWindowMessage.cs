using System;
using Cognifide.PowerShell.SitecoreIntegrations.Applications;
using Sitecore;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Framework;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages
{
    [Serializable]
    public class CloseWindowMessage : BasePipelineMessage, IMessage
    {
        /// <summary>
        ///     Shows a confirmation dialog.
        /// </summary>
        protected override void ShowUI()
        {
            if (!(Context.ClientPage.CodeBeside is PowerShellIse))
            {
                Context.ClientPage.ClientResponse.CloseWindow();
                Windows.Close();
            }
        }
    }
}