# Create-RestrictionProfileTemplate.ps1
# Creates the Restriction Profile Override template and settings folder via SPE remoting.
# Run after 'task deploy' with remoting enabled.
#
# Usage:
#   .\scripts\Create-RestrictionProfileTemplate.ps1
#
# After running, serialize the new items:
#   dotnet sitecore ser pull

param(
    [string]$ConnectionUri = "https://spe.dev.local"
)

$ErrorActionPreference = "Stop"

. "$PSScriptRoot\assert-prerequisites.ps1"

$moduleRoot = "$PSScriptRoot\..\modules\SPE"
Import-Module "$moduleRoot\SPE.psd1" -Force

$sharedSecret = Get-EnvValue "SPE_SHARED_SECRET"
$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $ConnectionUri

Write-Host "Creating Restriction Profile Override template and settings folder..." -ForegroundColor Cyan

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    $db = Get-Database -Name "master"

    # =========================================================================
    # 1. Create template under /sitecore/templates/Modules/PowerShell Console
    # =========================================================================
    $templateRoot = Get-Item -Path "master:/sitecore/templates/Modules/PowerShell Console"
    if (-not $templateRoot) {
        return "ERROR: Template root not found at /sitecore/templates/Modules/PowerShell Console"
    }

    $templateName = "Restriction Profile Override"
    $existingTemplate = Get-ChildItem -Path $templateRoot.Paths.FullPath -Recurse | Where-Object { $_.Name -eq $templateName }
    if ($existingTemplate) {
        return "Template '$templateName' already exists at $($existingTemplate.Paths.FullPath). Skipping creation."
    }

    # Create the template item
    $templateFolderId = "{0437FEE2-44C9-46A6-ABE9-28858D9FEE8C}" # /sitecore/templates/System/Templates/Template
    $template = New-Item -Path "$($templateRoot.Paths.FullPath)/$templateName" -ItemType "/sitecore/templates/System/Templates/Template"

    # Set base templates (Standard Template)
    $standardTemplateId = "{1930BBEB-7805-471A-A3BE-4858AC7CF696}"
    $template.Editing.BeginEdit()
    $template["__Base template"] = $standardTemplateId
    $template.Editing.EndEdit() | Out-Null

    # Create a section for the profile fields
    $section = New-Item -Path "$($template.Paths.FullPath)/Profile Settings" -ItemType "/sitecore/templates/System/Templates/Template section"

    # Create fields
    $singleLineId = "{A59C7034-47D4-4DFA-9958-1C49BB4D6808}"  # /sitecore/templates/System/Templates/Template field
    $fieldTemplate = "/sitecore/templates/System/Templates/Template field"

    # Field: Base Profile
    $f1 = New-Item -Path "$($section.Paths.FullPath)/Base Profile" -ItemType $fieldTemplate
    $f1.Editing.BeginEdit()
    $f1["Type"] = "Single-Line Text"
    $f1["Title"] = "Base Profile"
    $f1["__Short description"] = "Name of the config-based profile to extend (e.g., read-only, read-only-strict, content-editor)"
    $f1["Shared"] = "1"
    $f1.Editing.EndEdit() | Out-Null

    # Field: Additional Blocked Commands
    $f2 = New-Item -Path "$($section.Paths.FullPath)/Additional Blocked Commands" -ItemType $fieldTemplate
    $f2.Editing.BeginEdit()
    $f2["Type"] = "Multi-Line Text"
    $f2["Title"] = "Additional Blocked Commands"
    $f2["__Short description"] = "Commands to add to the blocklist, one per line. Only applies when the base profile uses blocklist mode."
    $f2["Shared"] = "1"
    $f2.Editing.EndEdit() | Out-Null

    # Field: Additional Allowed Commands
    $f3 = New-Item -Path "$($section.Paths.FullPath)/Additional Allowed Commands" -ItemType $fieldTemplate
    $f3.Editing.BeginEdit()
    $f3["Type"] = "Multi-Line Text"
    $f3["Title"] = "Additional Allowed Commands"
    $f3["__Short description"] = "Commands to add to the allowlist, one per line. Only applies when the base profile uses allowlist mode."
    $f3["Shared"] = "1"
    $f3.Editing.EndEdit() | Out-Null

    # Field: Trusted Script Paths
    $f4 = New-Item -Path "$($section.Paths.FullPath)/Trusted Script Paths" -ItemType $fieldTemplate
    $f4.Editing.BeginEdit()
    $f4["Type"] = "Multi-Line Text"
    $f4["Title"] = "Trusted Script Paths"
    $f4["__Short description"] = "Script Library paths to trust for this profile, one per line. Scripts at these paths run with elevated privileges."
    $f4["Shared"] = "1"
    $f4.Editing.EndEdit() | Out-Null

    # Field: Audit Level Override
    $f5 = New-Item -Path "$($section.Paths.FullPath)/Audit Level Override" -ItemType $fieldTemplate
    $f5.Editing.BeginEdit()
    $f5["Type"] = "Single-Line Text"
    $f5["Title"] = "Audit Level Override"
    $f5["__Short description"] = "Override audit level: None, Violations, Standard, Full. Leave blank to inherit from config profile."
    $f5["Shared"] = "1"
    $f5.Editing.EndEdit() | Out-Null

    # =========================================================================
    # 2. Create settings folder for override items
    # =========================================================================
    $settingsRoot = Get-Item -Path "master:/sitecore/system/Modules/PowerShell/Settings"
    if (-not $settingsRoot) {
        return "ERROR: Settings root not found"
    }

    $folderName = "Restriction Profiles"
    $existingFolder = Get-ChildItem -Path $settingsRoot.Paths.FullPath | Where-Object { $_.Name -eq $folderName }
    if (-not $existingFolder) {
        $folder = New-Item -Path "$($settingsRoot.Paths.FullPath)/$folderName" -ItemType "Common/Folder"
        "Created settings folder: $($folder.Paths.FullPath)"
    } else {
        "Settings folder already exists: $($existingFolder.Paths.FullPath)"
    }

    # Return template info for verification
    @{
        TemplateId = $template.ID.ToString()
        TemplatePath = $template.Paths.FullPath
        Fields = @(
            @{ Name = "Base Profile"; Id = $f1.ID.ToString() }
            @{ Name = "Additional Blocked Commands"; Id = $f2.ID.ToString() }
            @{ Name = "Additional Allowed Commands"; Id = $f3.ID.ToString() }
            @{ Name = "Trusted Script Paths"; Id = $f4.ID.ToString() }
            @{ Name = "Audit Level Override"; Id = $f5.ID.ToString() }
        )
    } | ConvertTo-Json -Depth 3
} -Raw

Write-Host $result

Stop-ScriptSession -Session $session
Write-Host "`nDone. Run 'dotnet sitecore ser pull' to serialize the new items to YAML." -ForegroundColor Green
