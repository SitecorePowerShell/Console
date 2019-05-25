using System;

namespace Spe.Commandlets.Data.Clones
{
    [Flags]
    public enum NotificationType
    {
        Notification = 0,
        ChildCreatedNotification = 1,
        FieldChangedNotification = 2,
        FirstVersionAddedNotification = 4,
        ItemMovedChildCreatedNotification = 8,
        ItemMovedChildRemovedNotification = 16,
        ItemMovedNotification = 32,
        ItemTreeMovedNotification = 64,
        ItemVersionNotification = 128,
        OriginalItemChangedTemplateNotification = 256,
        VersionAddedNotification = 512
    }
}