using System;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages
{
    [Serializable]
    public class ShellCommandInItemContextMessage : BasePipelineMessage, IMessage
    {
        private string itemUri;
        private string itemDb;
        private string command;
        private Handle jobHandle;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Sitecore.Jobs.AsyncUI.ConfirmMessage"/> class.
        /// 
        /// </summary>
        /// <param name="message">The message.</param>
        public ShellCommandInItemContextMessage(Item item, string command)
        {
            if (item != null)
            {
                itemUri = item.Uri.ToDataUri().ToString();
                itemDb = item.Database.Name;
            }
            jobHandle = JobContext.JobHandle;
            this.command = command;
        }

        /// <summary>
        /// Shows a confirmation dialog.
        /// 
        /// </summary>
        protected override void ShowUI()
        {
            CommandContext context = null;
            if (!string.IsNullOrEmpty(itemUri))
            {
                var item = Factory.GetDatabase(itemDb).GetItem(new DataUri(itemUri));
                context = new CommandContext(item);
            }
            else
            {
                context = new CommandContext();
            }
            context.Parameters.Add(Message.Parse(null,command).Arguments);
            Command shellCommand = CommandManager.GetCommand(command);
            if (shellCommand == null)
                return;
            shellCommand.Execute(context);
        }
    }
}