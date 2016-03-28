<#
    .SYNOPSIS
        Returns the item template and its base templates.

    .DESCRIPTION
        The Get-ItemTemplate command returns the item template and its base templates.

    .PARAMETER Item
        The item to be analysed.

    .PARAMETER Path
        Path to the item to be analysed.

    .PARAMETER Id
        Id of the item to be analysed.

    .PARAMETER Database
        Database containing the item to be analysed - required if item is specified with Id.
    
    .PARAMETER Recurse
        Return the template the item is based on and all of its base templates.
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Data.Items.TemplateItem

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Get-ItemField

    .LINK
        Set-ItemTemplate
        
    .LINK
        Add-BaseTemplate
        
    .LINK
        Remove-BaseTemplate                

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
	#Get template of /sitecore/conent/home item

        PS master:\> Get-ItemTemplate -Path master:\content\home

        BaseTemplates  : {Standard template}
        Fields         : {__Context Menu, __Display name, __Editor, __Editors...}
        FullName       : Sample/Sample Item
        Key            : sample item
        OwnFields      : {Title, Text, Image, State...}
        StandardValues : Sitecore.Data.Items.Item
        Database       : master
        DisplayName    : Sample Item
        Icon           : Applications/16x16/document.png
        ID             : {76036F5E-CBCE-46D1-AF0A-4143F9B557AA}
        InnerItem      : Sitecore.Data.Items.Item
        Name           : Sample Item

    .EXAMPLE
	# Get template of /sitecore/conent/home item and all of the templates its template is based on
        # then format it to only show the template name, path and Key

        PS master:\> Get-Item -Path master:/content/Home | Get-ItemTemplate -Recurse | ft Name, FullName, Key -auto

        Name              FullName                                 Key
        ----              --------                                 ---
        Sample Item       Sample/Sample Item                       sample item
        Standard template System/Templates/Standard template       standard template
        Advanced          System/Templates/Sections/Advanced       advanced
        Appearance        System/Templates/Sections/Appearance     appearance
        Help              System/Templates/Sections/Help           help
        Layout            System/Templates/Sections/Layout         layout
        Lifetime          System/Templates/Sections/Lifetime       lifetime
        Insert Options    System/Templates/Sections/Insert Options insert options
        Publishing        System/Templates/Sections/Publishing     publishing
        Security          System/Templates/Sections/Security       security
        Statistics        System/Templates/Sections/Statistics     statistics
        Tasks             System/Templates/Sections/Tasks          tasks
        Validators        System/Templates/Sections/Validators     validators
        Workflow          System/Templates/Sections/Workflow       workflow
#>
