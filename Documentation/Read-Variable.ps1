<#
    .SYNOPSIS
        Prompts user to provide values for variables required by the script to perform its operation.

    .DESCRIPTION
        Prompts user to provide values for variables required by the script to perform its operation.
        If user selects the "OK" button the command will return 'ok' as its value.
        If user selects the "Cancel" button or closes the window with the "x" button at the top-right corner of the dialog the command will return 'cancel' as its value.

    .PARAMETER Parameters
        Specifies the variables that value should be provided by the user. Each variable definition can have the following structure:
        - Name - the name of the PowerShell variable - without the $ sign
        - Value - the initial value of the variable - if the variable have not been created prior to launching the dialog - this will be its value unless the user changes it. if Value is not specified - the existing variable name will be used.
	- Title - The title for the variable shown above the variable editor.
        - Tooltip - The hint describing the parameter further - if the -ShowHints parameter is provided this value will show between the Variable Title and the variable editor.
	- Editor - If the default editor selected does not provide the functionality expected - you can specify this value to customize it (see examples)
	- Tab - if this parameter is specified on any Variable the multi-tab dialog will be used instead of a simple one. Provide the tab name on which the variable editor should appear.
	
        Variable type specific:
        - Root - for some Item selecting editors you can provide this to limit the selection to only part of the tree
        - Source - for some Item selecting editors you can provide this to parametrize the item selection editor. (Refer to examples for some sample usages)
        - Lines - for String variable you can select this parameter if you want to present the user with the multiline editor. The for this parameter is the number of lines that the editor will be configured with.
        - Domain - for user and role selectors you can limit the users & roles presented to only the domain - specified)

    .PARAMETER Description
        Dialog description displayed below the dialog title.

    .PARAMETER CancelButtonName
        Text shown on the cancel button.

    .PARAMETER OkButtonName
        Text shown on the OK button.

    .PARAMETER ShowHints
        Specifies whether the variable hints should be displayed. Hints are shown below each the variable title but above the variable editing control.

    .PARAMETER Title
        Dialog title - shown at the top of the dialog.

    .PARAMETER Width
        Dialog width.

    .PARAMETER Height
        Dialog width.
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        System.String

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Show-Alert
    .LINK
        Show-Application
    .LINK
        Show-Confirm
    .LINK
        Show-FieldEditor
    .LINK
        Show-Input
    .LINK
        Show-ListView
    .LINK
        Show-ModalDialog
    .LINK
        Show-YesNoCancel
    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        # For a good understanding of all the Property types the Read-Variable command accepts open the script located in the following item:
        # /Examples/User Interaction/Read-Variable - Sample
        # the script is located in the Script Library in the Master database.

    .EXAMPLE
        # Following is an example of a simple dialog asking user for various variable types.
        #
        # The type of some of the controls displayed to the user are iferred from the variable type (like the $item variable or DateTime)
        # The editors for some other are set by providing the "editor" value
        #
        $item = Get-Item master:\content\home
        $result = Read-Variable -Parameters `
            @{ Name = "someText"; Value="Some Text"; Title="Single Line Text"; Tooltip="Tooltip for singleline"}, 
            @{ Name = "multiText"; Value="Multiline Text"; Title="Multi Line Text"; lines=3; Tooltip="Tooltip for multiline"}, 
            @{ Name = "from"; Value=[System.DateTime]::Now.AddDays(-5); Title="Start Date"; Tooltip="Date since when you want the report to run"; Editor="date time"}, 
            @{ Name = "user"; Value=$me; Title="Select User"; Tooltip="Tooltip for user"; Editor="user multiple"},
            @{ Name = "item"; Title="Start Item"; Root="/sitecore/content/"} `
            -Description "This Dialog shows less editors, it doesn't need tabs as there is less of the edited variables" `
            -Title "Initialise various variable types (without tabs)" -Width 500 -Height 480 -OkButtonName "Proceed" -CancelButtonName "Abort" 

    .EXAMPLE
        # Following is an example of a multi tabbed dialog asking user for various variable types.
        #
        # The type of some of the controls displayed to the user are iferred from the variable type (like the $item variable or DateTime)
        # The editors for some other are set by providing the "editor" value
        #
        $item = Get-Item master:\content\home
        $result = Read-Variable -Parameters `
            @{ Name = "silent"; Value=$true; Title="Proceed Silently"; Tooltip="Check this if you don't want to be interrupted"; Tab="Simple"}, 
            @{ Name = "someText"; Value="Some Text"; Title="Text"; Tooltip="Just a single line of Text"; Tab="Simple"}, 
            @{ Name = "multiText"; Value="Multiline Text"; Title="Longer Text"; lines=3; Tooltip="You can put multi line text here"; Tab="Simple"}, 
            @{ Name = "number"; Value=110; Title="Integer"; Tooltip="I need this number too"; Tab="Simple"}, 
            @{ Name = "fraction"; Value=1.1; Title="Float"; Tooltip="I'm just a bit over 1"; Tab="Simple"}, 
            @{ Name = "from"; Value=[System.DateTime]::Now.AddDays(-5); Title="Start Date"; Tooltip="Date since when you want the report to run"; Editor="date time"; Tab="Time"}, 
            @{ Name = "fromDate"; Value=[System.DateTime]::Now.AddDays(-5); Title="Start Date"; Tooltip="Date since when you want the report to run"; Editor="date"; Tab="Time"}, 
            @{ Name = "item"; Title="Start Item"; Root="/sitecore/content/"; Tab="Items"}, 
            @{ Name = "items"; Title="Bunch of Templates"; 
                Source="DataSource=/sitecore/templates&DatabaseName=master&IncludeTemplatesForDisplay=Node,Folder,Template,Template Folder&IncludeTemplatesForSelection=Template"; 
                editor="treelist"; Tab="Items"}, 
            @{ Name = "items2"; Title="Bunch of Templates"; 
                Source="DataSource=/sitecore/templates&DatabaseName=master&IncludeTemplatesForDisplay=Node,Folder,Template,Template Folder&IncludeTemplatesForSelection=Template"; 
                editor="multilist"; Tab="More Items"}, 
            @{ Name = "items3"; Title="Pick One Template"; 
                Source="DataSource=/sitecore/templates&DatabaseName=master&IncludeTemplatesForDisplay=Node,Folder,Template,Template Folder&IncludeTemplatesForSelection=Template"; 
                editor="droplist"; Tab="More Items"}, 
            @{ Name = "user"; Value=$me; Title="Select User"; Tooltip="Tooltip for user"; Editor="user multiple"; Tab="Rights"}, 
            @{ Name = "role"; Title="Select Role"; Tooltip="Tooltip for role"; Editor="role multiple"; Domain="sitecore"; Tab="Rights"}, `
            @{ Name = "userOrRole"; Title="Select User or Role"; Tooltip="Tooltip for role"; Editor="user role multiple"; Domain="sitecore"; Tab="Rights" } `
            -Description "This Dialog shows all available editors in some configurations, the properties are groupped into tabs" `
            -Title "Initialise various variable types (with tabs)" -Width 600 -Height 640 -OkButtonName "Proceed" -CancelButtonName "Abort" -ShowHints
        if($result -ne "ok")
        {
            Exit
        }
        
#>
