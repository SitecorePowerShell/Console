$VerbosePreference = "SilentlyContinue"
Import-Module -Name SPE -Force
$VerbosePreference = "Continue"

# If you need to connect to more than one instance of Sitecore add it to the list.
$instanceUrls = @("https://spe.dev.local","https://spe.dev.local")
$session = New-ScriptSession -Username michael -Password b -ConnectionUri $instanceUrls

Write-Host "Testing session with no parameters" -ForegroundColor Yellow
Invoke-RemoteScript -Session $session -ScriptBlock { $env:computername }

Write-Host "Testing without a session and with no parameters" -ForegroundColor Yellow
Invoke-RemoteScript -Username michael -Password b -ConnectionUri $instanceUrls -ScriptBlock { $env:computername }

Write-Host "Testing session with the `$Using variable" -ForegroundColor Yellow
$identity = "michael"
Invoke-RemoteScript -Session $session -ScriptBlock {
    Get-User -Id $using:identity
}

Stop-ScriptSession -Session $session