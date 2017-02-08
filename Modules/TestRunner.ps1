Import-Module Pester -Force

$scriptParameters = @{ Path="."; Parameters=@{ protocolHost="http://sitecore81"}}
if($env:USERDOMAIN -eq "Michael-Laptop") {
    $scriptParameters = @{ Path="."; Parameters=@{ protocolHost="https://spe.dev.local"}}
}
Invoke-Pester -Script $scriptParameters