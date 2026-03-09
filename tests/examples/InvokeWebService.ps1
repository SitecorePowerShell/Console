#$VerbosePreference = "Continue"

Import-Module -Name SPE -Force

if(!$credential) {
    $credential = Get-Credential
}

$session = New-ScriptSession -Username "admin" -Password "b" -ConnectionUri "http://console/" -Credential $credential

$script1 = {
    [Sitecore.Security.Accounts.User]$user = Get-User -Identity admin
    $user
}

Invoke-RemoteScript -ScriptBlock $script1 -Session $session


$script2 = {
    $params.date.ToString()
}

$args = @{
    "date" = [datetime]::Now
}

#Invoke-RemoteScript -ScriptBlock $script2 -Session $session -ArgumentList $args


$script3 = {
    Import-Function -Name Invoke-ApiScript

    Invoke-ApiScript -ScriptBlock { 
        Get-Item -Path master:\content\home | Select-Object -Property Name, ItemPath
        Get-ChildItem -Path master:\content\home -Recurse | Select-Object -Property Name, ItemPath
    }
}

#Invoke-RemoteScript -ScriptBlock $script3 -Session $session


