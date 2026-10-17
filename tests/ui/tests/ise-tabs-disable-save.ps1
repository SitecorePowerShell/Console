# Counterpart of ise-tabs-enable-save.ps1: turn "save active tabs" back off
# for the current user so later tests start from a single clean tab.
$settings = [Spe.Core.Settings.ApplicationSettings]::GetInstance("ISE")
$settings.GetType().GetProperty("SaveActiveTabs").SetValue($settings, $false)
$settings.Save()
[Spe.Core.Settings.ApplicationSettings]::ReloadInstance("ISE", $true)
$settings = [Spe.Core.Settings.ApplicationSettings]::GetInstance("ISE")
Write-Host "SaveActiveTabs after: $($settings.SaveActiveTabs)"
