<#
    .SYNOPSIS
        Returns all the items linked to the specified item..

    .DESCRIPTION
        The Get-ItemReference command returns all items linked to the specified item. If -ItemLink parameter is used the command will return links rather than items.

    .PARAMETER ItemLink
        Return ItemLink that define both source and target of a link rather than items that are being linked to from the specified item.

    .PARAMETER Item
        The item to be analysed.

    .PARAMETER Path
        Path to the item to be analysed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Id
        Id of the item to be analysed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Database
        Database containing the item to be analysed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Language
        If you need the item in specific Language you can specify it with this parameter. Globbing/wildcard supported.    
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Data.Items.Item
        Sitecore.Links.ItemLink

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Get-ItemReferrer

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\>Get-ItemReference -Path master:\content\home
         
        Name                             Children Languages                Id                                     TemplateName
        ----                             -------- ---------                --                                     ------------
        Home                             True     {en, de-DE, es-ES, pt... {110D559F-DEA5-42EA-9C1C-8A5DF7E70EF9} Sample Item
        Home                             True     {en, de-DE, es-ES, pt... {110D559F-DEA5-42EA-9C1C-8A5DF7E70EF9} Sample Item

    .EXAMPLE
        PS master:\>Get-Item master:\content\home | Get-ItemReference -ItemLink
         
        SourceItemLanguage : en
        SourceItemVersion  : 1
        TargetItemLanguage :
        TargetItemVersion  : 0
        SourceDatabaseName : master
        SourceFieldID      : {F685964D-02E1-4DB6-A0A2-BFA59F5F9806}
        SourceItemID       : {110D559F-DEA5-42EA-9C1C-8A5DF7E70EF9}
        TargetDatabaseName : master
        TargetItemID       : {110D559F-DEA5-42EA-9C1C-8A5DF7E70EF9}
        TargetPath         : /sitecore/content/Home
#>
