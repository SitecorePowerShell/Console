using System;
using Sitecore;
using Sitecore.Jobs.AsyncUI;

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
            Context.ClientPage.ClientResponse.CloseWindow();
        }
    }
}