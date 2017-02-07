Import-Module Pester -Force
Invoke-Pester -Script @{ Path="."; Parameters=@{ protocolHost="http://sitecore81"}}