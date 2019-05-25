using System;
using System.Management.Automation;
using Sitecore.Configuration;
using Sitecore.Data.Clones;
using Sitecore.Data.Items;
using Spe.Core.Utility;

namespace Spe.Commands.Data.Clones
{
    [Cmdlet(VerbsCommunications.Receive, "ItemCloneNotification", SupportsShouldProcess = true)]
    public class ReceiveItemCloneNotificationCommand : BaseItemCloneNotificationCommand
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        [Parameter(ParameterSetName = "Item from Notification")]
        public Notification Notification { get; set; }

        [Parameter(Mandatory = true)]
        public NotificationAction Action { get; set; } = NotificationAction.None;

        protected override void ProcessRecord()
        {
            if (Notification != null)
            {
                FilterNotification(Notification);
            }
            else
            {
                base.ProcessRecord();
            }
        }

        protected override void ProcessNotification(Notification notification)
        {
            var clone = Factory.GetDatabase(notification.Uri.DatabaseName).GetItem(notification.Uri.ToDataUri());
            if (ShouldProcess(clone.GetProviderPath(), $"{Action} changes on clone that were introduced on original item"))
            {
                switch (Action)
                {
                    case (NotificationAction.Accept):
                        notification.Accept(clone);
                        break;
                    case (NotificationAction.Reject):
                        notification.Reject(clone);
                        break;
                    case (NotificationAction.Dismiss):
                        try
                        {
                            // dismiss does not seem to be universally supported by all Sitecore versions
                            // wrapper introduced no avoid late method binding error
                            DismissNotification(notification, clone);
                        }
                        catch (Exception ex)
                        {
                            WriteError(ex, ErrorIds.MethodNotFound, ErrorCategory.NotImplemented, notification);
                        }
                        break;
                }
            }
        }

        private void DismissNotification(Notification notification, Item clone)
        {
            notification.Dismiss(clone);
        }
    }
}