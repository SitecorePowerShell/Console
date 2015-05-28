using System;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Commandlets.Interactive.Messages
{
    [Serializable]
    public class ShellCommandInItemContextMessage : BasePipelineMessage, IMessageWithResult
    {
        private readonly string command;
        private readonly string itemDb;
        private readonly string itemUri;
        private readonly Handle jobHandle;
        [NonSerialized] private readonly MessageQueue messageQueue;

        public ShellCommandInItemContextMessage(Item item, string command)
        {
            if (JobContext.IsJob)
            {
                jobHandle = JobContext.JobHandle;
            }

            messageQueue = new MessageQueue();
            if (item != null)
            {
                itemUri = item.Uri.ToDataUri().ToString();
                itemDb = item.Database.Name;
            }
            jobHandle = JobContext.JobHandle;
            this.command = command;
        }

        public MessageQueue MessageQueue
        {
            get { return messageQueue; }
        }

        /// <summary>
        ///     Shows a confirmation dialog.
        /// </summary>
        protected override void ShowUI()
        {
            CommandContext context;
            if (!string.IsNullOrEmpty(itemUri))
            {
                var item = Factory.GetDatabase(itemDb).GetItem(new DataUri(itemUri));
                context = new CommandContext(item);
            }
            else
            {
                context = new CommandContext();
            }
            context.Parameters.Add(Message.Parse(null, command).Arguments);
            if (jobHandle != null)
            {
                context.Parameters.Add("jobHandle", jobHandle.ToString());
            }
            var shellCommand = CommandManager.GetCommand(command);
            if (shellCommand == null)
                return;
            shellCommand.Execute(context);
        }
    }
}