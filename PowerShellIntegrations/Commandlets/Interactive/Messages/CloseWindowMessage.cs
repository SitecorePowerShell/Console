using System;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Resources.Media;
using Sitecore.Shell.Framework;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages
{
    [Serializable]
    public class CloseWindowMessage : BasePipelineMessage, IMessage
    {
        public CloseWindowMessage()
        {
        }

        /// <summary>
        /// Shows a confirmation dialog.
        /// 
        /// </summary>
        protected override void ShowUI()
        {
            Context.ClientPage.ClientResponse.CloseWindow();
        }
    }
}