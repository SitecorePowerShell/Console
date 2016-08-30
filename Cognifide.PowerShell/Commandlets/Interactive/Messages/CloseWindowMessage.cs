using System;
using Cognifide.PowerShell.Client.Applications;
using Sitecore;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Framework;

namespace Cognifide.PowerShell.Commandlets.Interactive.Messages
{
    [Serializable]
    public class CloseWindowMessage : BasePipelineMessage
    {
        /// <summary>
        ///     Shows a confirmation dialog.
        /// </summary>
        protected override void ShowUI()
        {
            if (!(Context.ClientPage.CodeBeside is PowerShellIse))
            {
                Context.ClientPage.ClientResponse.CloseWindow();
                Sitecore.Shell.Framework.Windows.Close();
            }
        }
    }
}