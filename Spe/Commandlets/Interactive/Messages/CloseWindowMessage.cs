using System;
using Sitecore;
using Sitecore.Jobs.AsyncUI;
using Spe.Client.Applications;

namespace Spe.Commandlets.Interactive.Messages
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