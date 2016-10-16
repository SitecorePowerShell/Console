<#
    .SYNOPSIS
        New-UsingBlock.

    .DESCRIPTION
        New-UsingBlock.


    .PARAMETER InputObject
        Object that should be disposed after the Script block is executed.

    .PARAMETER ScriptBlock
        Script to be executed within the "Using" context.
    
    .INPUTS
        System.IDisposable
    
    .OUTPUTS
	void        

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
    	#Assuming all items under /sitecore/content/home have both 'Title' and 'MetaTitle' fields...
	#Using New-UsingBlock to bulk update items under /sitecore/Content/ 
	#to have their 'MetaTitle' field to be equal to the 'Title' field

        New-UsingBlock (New-Object Sitecore.Data.BulkUpdateContext) {
	    foreach ( $item in (Get-ChildItem -Path master:\Content\Home -Recurse -WithParent) ) {
    	        $item."MetaTitle" = $item.Title
	        }
	    }

    .EXAMPLE
	#Using New-UsingBlock to perform a test with UserSwitcher - checking whether an anonymous user can change a field
	#The test should end up showing the error as below and the Title should not be changed!

	$anonymous = Get-User -Identity "extranet\Anonymous"
	$testItem = Get-Item -Path master:\Content\Home

	New-UsingBlock (New-Object Sitecore.Security.Accounts.UserSwitcher $anonymous) {
	    $testItem.Title = "If you can see this title it means that anonymous users can change this item!"
	}


	New-UsingBlock : Exception setting "Title": "Exception calling "Modify" with "3" argument(s): "The current user does not have write access to this item. User: extranet\Anonymous, Item: Home ({110D559F-DEA5-42EA-9C1C-8A5DF7E70EF9})""
	At line:3 char:1
	+ New-UsingBlock (New-Object Sitecore.Security.Accounts.UserSwitcher $a ...
	+ ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
	    + CategoryInfo          : NotSpecified: (:) [New-UsingBlock], SetValueInvocationException
	    + FullyQualifiedErrorId : ScriptSetValueRuntimeException,Cognifide.PowerShell.Commandlets.Data.NewUsingBlockCommand


#>
