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
    public class ShellCommandInItemContextMessage : BasePipelineMessageWithResult
    {
        private readonly string command;
        private readonly string itemDb;
        private readonly string itemUri;

        public ShellCommandInItemContextMessage(Item item, string command)
        {
            if (item != null)
            {
                itemUri = item.Uri.ToDataUri().ToString();
                itemDb = item.Database.Name;
            }
            this.command = command;
        }

        protected override bool WaitForPostBack => false;

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
            if (JobHandle != null)
            {
                context.Parameters.Add("jobHandle", JobHandle.ToString());
            }
            var shellCommand = CommandManager.GetCommand(command);
            if (shellCommand == null)
                return;
            shellCommand.Execute(context);
        }

        protected override object ProcessResult(bool hasResult, string result)
        {
            return result;
        }
    }
}