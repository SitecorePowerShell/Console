# Turn on "save active tabs" for the current user so the ISE persists the
# open tab set across reloads. SaveActiveTabs has a private setter, so it is
# flipped through reflection and the cached instance is dropped afterwards.
$settings = [Spe.Core.Settings.ApplicationSettings]::GetInstance("ISE")
Write-Host "SaveActiveTabs before: $($settings.SaveActiveTabs)"
$settings.GetType().GetProperty("SaveActiveTabs").SetValue($settings, $true)
$settings.Save()
[Spe.Core.Settings.ApplicationSettings]::ReloadInstance("ISE", $true)
$settings = [Spe.Core.Settings.ApplicationSettings]::GetInstance("ISE")
Write-Host "SaveActiveTabs after: $($settings.SaveActiveTabs)"
