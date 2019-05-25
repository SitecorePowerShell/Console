using System;
using System.Management.Automation;
using Sitecore.Data.Clones;
using Sitecore.Data.Items;

namespace Spe.Commands.Data.Clones
{
    public abstract class BaseItemCloneNotificationCommand : BaseItemCommand
    {

        [Parameter()]
        public virtual NotificationType NotificationType { get; set; }

        protected override void ProcessItem(Item item)
        {
            var notificationProvider = item.Database.NotificationProvider;
            if (notificationProvider == null)
            {
                // No notification provider exists for db
                WriteError(new ErrorRecord(new NotSupportedException(
                    $"The database {item.Database.Name} does not have a configured notification provider"),
                    "sitecore_no_item_notification_provider", ErrorCategory.InvalidOperation, item));
                return;
            }

            foreach (var notification in notificationProvider.GetNotifications(item))
            {
                FilterNotification(notification);
            }
        }

        protected virtual void FilterNotification(Notification notification)
        {
            var itemNotificationType =
                (NotificationType) Enum.Parse(typeof(NotificationType), notification.GetType().Name, true);
            if (NotificationType == NotificationType.Notification ||
                (itemNotificationType & NotificationType) != NotificationType.Notification)
            {
                ProcessNotification(notification);
            }
        }

        protected abstract void ProcessNotification(Notification notification);

    }
}