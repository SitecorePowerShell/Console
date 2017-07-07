<#
    .SYNOPSIS
        Executes raw JavaScript in the browser.

    .DESCRIPTION
        Executes JavaScript in the browser. Useful for logging messages to the console.

    .PARAMETER Script
        TODO: Provide description for this parameter
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        None

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .EXAMPLE
        # Running the following example will write messages to the browser console.

        1..5 | %{ 
            Start-Sleep -Seconds 1; 
            Invoke-JavaScript -Script "console.log('Hello World! Call #$($_) from PowerShell...');" 
        }

        Invoke-JavaScript -Script "alert('hello from powershell');"
#>
