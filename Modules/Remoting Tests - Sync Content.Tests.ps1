Import-Module -Name "SPE" -Force

function Copy-RainbowContent {
    param(
        [string]$LocalUrl,
        [string]$RemoteUrl,
        [string]$Username,
        [string]$Password,
        [string]$RootId,
        [switch]$Recurse
    )

    $localSession = New-ScriptSession -user $Username -pass $Password -conn $LocalUrl
    $remoteSession = New-ScriptSession -user $Username -pass $Password -conn $RemoteUrl

    $parentId = $RootId
    $shouldRecurse = $Recurse.IsPresent
    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    $rainbowYaml = Invoke-RemoteScript -ScriptBlock {
        if($using:shouldRecurse) {
            Get-ChildItem -Path "master:" -ID $using:parentId -Recurse -WithParent | ConvertTo-RainbowYaml
        } else {
            Get-Item -Path "master:" -ID $using:parentId | ConvertTo-RainbowYaml
        }        
    } -Session $localSession -Raw
    $watch.Stop()
    $watch.ElapsedMilliseconds / 1000

    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    $result2 = Invoke-RemoteScript -ScriptBlock {
        [regex]::Split($using:rainbowYaml, "(?=---)") | Where-Object { ![string]::IsNullOrEmpty($_) } | Import-RainbowItem

        Import-Function -Name Resolve-Error
        Resolve-Error $Error[0]
    } -Session $remoteSession -Raw
    $watch.Stop()
    $watch.ElapsedMilliseconds / 1000
}

$copyProps = @{
    LocalUrl = "https://spe.dev.local"
    RemoteUrl = "http://sc827"
    Username = "admin"
    Password = "b"    
}

# Home\Delete Me
Copy-RainbowContent @copyProps -RootId "{A6649F02-B4B6-4985-8FD5-7D40CA9E829F}"

# Images
Copy-RainbowContent @copyProps -RootId "{15451229-7534-44EF-815D-D93D6170BFCB}"

<#
Import-Module -Name SitecoreSidekick -Force -Verbose
$params = @{
    LocalUrl = "http://demo2"
    RemoteUrl = "http://demo"
    SharedSecret = "3a4e9aad-5d61-42ae-b262-de0e13f3b576"
    EventDisabler = $true
    BulkUpdate = $true
    PullParent = $true
}
#MAKE THE CONTENT FROM ONE SERVER MATCH THE OTHER EXACTLY
Copy-SKContent @params -RootId '{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}' -Children -Overwrite -RemoveLocalNotInRemote
#MOVE ALL CONTENT FROM ONE SERVER TO THE OTHER AND ALLOWING UNIQUE ITEMS AT THE CONSUMER
Copy-SKContent @params -RootId '{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}' -Children -Overwrite
#MOVE ONLY NEW CONTENT FROM ONE SERVER TO THE OTHER WITHOUT UPDATING MODIFIED
Copy-SKContent @params -RootId '{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}' -Children
#ONLY MOVE THE INDIVIDUAL ITEMS AND NOT CHILDREN
Copy-SKContent @params -RootId '{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}' -Overwrite
#>