<#
    .SYNOPSIS
        Updates references to specified item to point to a new provided in the -NewTarget ore removes links to the item.

    .DESCRIPTION
        The cmdlet manipulates link to a specific item. The target item can be provided as an Item object or through Path/ID.
        it does not modifies the item itself but rather other items that link to it.
        If the -RemoveLink parameter is used the link will be removed rather than modified.
        To deliver more fine grained filtering you can provide ItemLink using the -Link parameter. You can obtain ItemLinks using Get-ItemReferrer or Get-ItemReference cmdlets.
        Consult Examples for specific use cases of each approach.

    .PARAMETER Link
        ItemLink retrieved from the Link database. Use this parameter to do more granular filtering.

    .PARAMETER NewTarget
        New item the links should be pointing to

    .PARAMETER RemoveLink
        If provided, removes all links to the current target item.

    .PARAMETER Item
        The current item to be relinked.

    .PARAMETER Path
        Path to the current item to be relinked - can work with Language parameter to narrow the publication scope.

    .PARAMETER Id
        Id of the current item to be relinked - can work with Language parameter to specify the language other than current session language. Requires the Database parameter to be specified.

    .PARAMETER Database
        Database containing the current item to be relinked.

    .PARAMETER Language
        If you need the current item to be relinked in specific Language You can specify it with this parameter. Globbing/wildcard supported.

    .PARAMETER WhatIf
        Shows what would happen if the cmdlet runs. The cmdlet is not run.

    .PARAMETER Confirm
        Prompts you for confirmation before running the cmdlet.    
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Get-ItemReferrer

    .LINK
        Get-ItemReference

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        # This example covers global operations
        #
        # Assuming Sitecore PowerShell Extensions 4.2 or newer is installed
        # Assuming your Home has an "Image" field of type "Image"
        # Assuming you have second item next to Home called Home2 that has an "Image" field of type "Image"

        $coverImage = Get-Item 'master:\media library\Default Website\cover'
        $scLogoImage = Get-item 'master:\media library\Default Website\sc_logo'

        Write-Host "`nReset 'home', 'child' and 'home2' to link to 'cover'- 3 items" -Foreground Magenta
        (Get-item master:\content\home).Image = $coverImage
        (Get-item master:\content\Home\child).Image = $coverImage
        (Get-item master:\content\home2).Image = $coverImage
        
        Get-ItemReferrer -Item $coverImage            
        
        Write-Host "`nRelinking all instances of 'cover' image to  'sc_logo'" -Foreground Yellow
        $coverImage | Update-ItemReferrer -NewTarget $scLogoImage
        
        Write-Host "`n'cover' should no longer have links leading to it - 0 items " -Foreground Red
        $coverImage | Get-ItemReferrer 
        
        Write-Host "`n'sc_logo' should now be linked from all - 3 items" -Foreground Green
        $scLogoImage | Get-ItemReferrer
        
        Write-Host "`nRemoving links to 'sc_logo' from all items" -Foreground Yellow
        $scLogoImage | Update-ItemReferrer -RemoveLink
        
        Write-Host "`n'sc_logo' should have no links to it - 0 items" -Foreground Red
        $scLogoImage | Get-ItemReferrer
        
    .EXAMPLE
        # This example covers more fine-grained filtered approach to removing links
        #
        # Assuming Sitecore PowerShell Extensions 4.2 or newer is installed
        # Assuming your Home has an "Image" field of type "Image"
        # Assuming you have second item next to Home called Home2 that has an "Image" field of type "Image"

        $coverImage = Get-Item 'master:\media library\Default Website\cover'
        $scLogoImage = Get-item 'master:\media library\Default Website\sc_logo'
        
        Write-Host "`nReset 'home', 'child' and 'home2' to link to 'cover'- 3 items" -Foreground Magenta
        (Get-item master:\content\home).Image = $coverImage
        (Get-item master:\content\Home\child).Image = $coverImage
        (Get-item master:\content\home2).Image = $coverImage
        
        Get-ItemReferrer -Item $coverImage            
        
        Write-Host "`nRemove links to the 'cover' image from all items under 'master:\content\home'" -Foreground Yellow
        Get-ChildItem master:\content\home -WithParent -Recurse |   # get items under home
            Get-ItemReference -ItemLink |                           # get all items that they are refering to
            ? { $_.TargetItemID -eq $coverImage.ID } |              # filter only references to $coverImage
            Update-ItemReferrer -RemoveLink                         # remove links
        
        Write-Host "`n'cover' should have 1 link leading from 'home2'" -Foreground Green
        $coverImage | Get-ItemReferrer
        
    .EXAMPLE
        # This example covers more fine-grained filtered approach to removing links
        #
        # Assuming Sitecore PowerShell Extensions 4.2 or newer is installed
        # Assuming your Home has an "Image" field of type "Image"
        # Assuming you have second item next to Home called Home2 that has an "Image" field of type "Image"

        $coverImage = Get-Item 'master:\media library\Default Website\cover'
        $scLogoImage = Get-item 'master:\media library\Default Website\sc_logo'
        
        Write-Host "`nReset 'home', 'child' and 'home2' to link to 'cover'- 3 items" -Foreground Magenta
        (Get-item master:\content\home).Image = $coverImage
        (Get-item master:\content\Home\child).Image = $coverImage
        (Get-item master:\content\home2).Image = $coverImage
        
        Get-ItemReferrer -Item $coverImage            
        
        Write-Host "`nUpdate all links to 'cover' image to point to 'sc_logo' from all immediate children of /sitecore/content" -Foreground Yellow
        Get-ChildItem master:\content |                     # get items immediately under 'under home'content'
            Get-ItemReference -ItemLink |                   # get all items that they are refering to
            ? { $_.TargetItemID -eq $coverImage.ID } |      # filter only references to $coverImage
            Update-ItemReferrer -NewTarget $scLogoImage     # point them to 'sc_logo' image
        
        Write-Host "`n'cover' should have link from home2/child - 1 item" -Foreground Green
        $coverImage | Get-ItemReferrer
        
        Write-Host "`n'sc_logo' should have links leading from 'home' and 'home2' - 2 items" -Foreground Green
        $scLogoImage | Get-ItemReferrer
#>
