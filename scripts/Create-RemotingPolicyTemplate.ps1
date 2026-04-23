# Create-RemotingPolicyTemplate.ps1
# Creates the Remoting Policy template and settings folder via SPE remoting.
# Run after 'task deploy' with remoting enabled.
#
# Usage:
#   .\scripts\Create-RemotingPolicyTemplate.ps1
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

Write-Host "Creating Remoting Policy template and settings folder..." -ForegroundColor Cyan

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    $db = Get-Database -Name "master"

    # =========================================================================
    # 1. Create template under /sitecore/templates/Modules/PowerShell Console/Remoting
    # =========================================================================
    $remotingRoot = Get-Item -Path "master:/sitecore/templates/Modules/PowerShell Console/Remoting"
    if (-not $remotingRoot) {
        return "ERROR: Remoting template root not found"
    }

    $templateName = "Remoting Policy"
    $existingTemplate = Get-ChildItem -Path $remotingRoot.Paths.FullPath | Where-Object { $_.Name -eq $templateName }
    if ($existingTemplate) {
        return "Template '$templateName' already exists at $($existingTemplate.Paths.FullPath). Skipping creation."
    }

    # Create the template item
    $template = New-Item -Path "$($remotingRoot.Paths.FullPath)/$templateName" -ItemType "/sitecore/templates/System/Templates/Template"

    # Set base templates (Standard Template)
    $standardTemplateId = "{1930BBEB-7805-471A-A3BE-4858AC7CF696}"
    $template.Editing.BeginEdit()
    $template["__Base template"] = $standardTemplateId
    $template.Editing.EndEdit() | Out-Null

    # Create a section for the policy fields
    $section = New-Item -Path "$($template.Paths.FullPath)/Policy Settings" -ItemType "/sitecore/templates/System/Templates/Template section"
    $fieldTemplate = "/sitecore/templates/System/Templates/Template field"

    # Field: Enabled
    $f1 = New-Item -Path "$($section.Paths.FullPath)/Enabled" -ItemType $fieldTemplate
    $f1.Editing.BeginEdit()
    $f1["Type"] = "Checkbox"
    $f1["Title"] = "Enabled"
    $f1["__Short description"] = "When checked, this policy is active and can be assigned to API Keys."
    $f1["Shared"] = "1"
    $f1.Editing.EndEdit() | Out-Null

    # Field: FullLanguage
    $f2 = New-Item -Path "$($section.Paths.FullPath)/FullLanguage" -ItemType $fieldTemplate
    $f2.Editing.BeginEdit()
    $f2["Type"] = "Checkbox"
    $f2["Title"] = "Full Language"
    $f2["__Short description"] = "When checked, scripts run in FullLanguage mode. When unchecked (default), ConstrainedLanguage mode is enforced."
    $f2["Shared"] = "1"
    $f2.Editing.EndEdit() | Out-Null

    # Field: AllowedCommands
    $f3 = New-Item -Path "$($section.Paths.FullPath)/AllowedCommands" -ItemType $fieldTemplate
    $f3.Editing.BeginEdit()
    $f3["Type"] = "Multi-Line Text"
    $f3["Title"] = "Allowed Commands"
    $f3["__Short description"] = "Commands permitted for inline scripts (remoting endpoint), one per line. Approved Script Library items bypass this list."
    $f3["Shared"] = "1"
    $f3.Editing.EndEdit() | Out-Null

    # Field: ApprovedScripts
    $f4 = New-Item -Path "$($section.Paths.FullPath)/ApprovedScripts" -ItemType $fieldTemplate
    $f4.Editing.BeginEdit()
    $f4["Type"] = "Treelist"
    $f4["Title"] = "Approved Scripts"
    $f4["__Short description"] = "Script Library items that bypass restrictions when invoked by reference (restfulv1/v2). Does not apply to inline scripts."
    $f4["Source"] = "DataSource=/sitecore/system/Modules/PowerShell/Script Library&IncludeTemplatesForSelection=PowerShell Script"
    $f4["Shared"] = "1"
    $f4.Editing.EndEdit() | Out-Null

    # Field: AuditLevel
    $f5 = New-Item -Path "$($section.Paths.FullPath)/AuditLevel" -ItemType $fieldTemplate
    $f5.Editing.BeginEdit()
    $f5["Type"] = "Droplist"
    $f5["Title"] = "Audit Level"
    $f5["__Short description"] = "Controls logging verbosity: None, Violations (default), Standard, Full."
    $f5["Source"] = "None|Violations|Standard|Full"
    $f5["Shared"] = "1"
    $f5.Editing.EndEdit() | Out-Null

    # =========================================================================
    # 2. Create settings folder for policy items
    # =========================================================================
    $securityRoot = Get-Item -Path "master:/sitecore/system/Modules/PowerShell/Settings/Access"
    if (-not $securityRoot) {
        return "ERROR: Security root not found"
    }

    $folderName = "Policies"
    $existingFolder = Get-ChildItem -Path $securityRoot.Paths.FullPath | Where-Object { $_.Name -eq $folderName }
    if (-not $existingFolder) {
        $folder = New-Item -Path "$($securityRoot.Paths.FullPath)/$folderName" -ItemType "Common/Folder"
        "Created settings folder: $($folder.Paths.FullPath)"
    } else {
        "Settings folder already exists: $($existingFolder.Paths.FullPath)"
    }

    # Return template info for verification
    @{
        TemplateId = $template.ID.ToString()
        TemplatePath = $template.Paths.FullPath
        Fields = @(
            @{ Name = "Enabled"; Id = $f1.ID.ToString() }
            @{ Name = "FullLanguage"; Id = $f2.ID.ToString() }
            @{ Name = "AllowedCommands"; Id = $f3.ID.ToString() }
            @{ Name = "ApprovedScripts"; Id = $f4.ID.ToString() }
            @{ Name = "AuditLevel"; Id = $f5.ID.ToString() }
        )
    } | ConvertTo-Json -Depth 3
} -Raw

Write-Host $result

Stop-ScriptSession -Session $session
Write-Host "`nDone. Run 'dotnet sitecore ser pull' to serialize the new items to YAML." -ForegroundColor Green
