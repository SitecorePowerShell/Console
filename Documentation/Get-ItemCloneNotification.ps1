<#
    .SYNOPSIS
        Get-ItemCloneNotification.

    .DESCRIPTION
        Get-ItemCloneNotification.

    .PARAMETER NotificationType
        The notification type can be one of the following:

        - Notification
        - ChildCreatedNotification
        - FieldChangedNotification
        - FirstVersionAddedNotification
        - ItemMovedChildCreatedNotification
        - ItemMovedChildRemovedNotification
        - ItemMovedNotification
        - ItemTreeMovedNotification
        - ItemVersionNotification
        - OriginalItemChangedTemplateNotification
        - VersionAddedNotification

    .PARAMETER Language
        If you need the item in specific Language You can specify it with this parameter. Globbing/wildcard supported.

    .PARAMETER Item
        The item to be processed.

    .PARAMETER Path
        Path to the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Id
        Id of the item to be processed - can work with Language parameter to specify the language other than current session language. Requires the Database parameter to be specified.

    .PARAMETER Database
        Database containing the item to be fetched with Id parameter.
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Data.Clones.Notification

    .NOTES
        Help Author: Adam Najmanowicz, Michael West
#>
