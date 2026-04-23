<#
.SYNOPSIS
    Follow-up to Migrate-ToRemotingClientsV1.ps1 that:
    - removes the Audit section and its three fields from the base
      Remoting Client template (decided out of v1 scope)
    - renames the content folder
        /sitecore/system/Modules/PowerShell/Settings/Access/API Keys
      to "Remoting Clients"
    - renames and updates the two Content Editor scripts under
        /sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Remoting
      so their visible labels and dialog titles use "Shared Secret Client"
      instead of "Remoting API Key"

.DESCRIPTION
    Runs inside a remoting session via Invoke-RemoteScript. Idempotent.
    After running, 'task pull' re-serializes the tree.
#>
$ErrorActionPreference = 'Stop'

function Write-Step {
    param([string]$Message)
    Write-Host "==> $Message" -ForegroundColor Cyan
}

# Step 1: Delete Audit fields + section on base ----------------------------

Write-Step "Remove Audit section from base Remoting Client template"
$auditSection = 'master:/sitecore/templates/Modules/PowerShell Console/Remoting/Remoting Client/Audit'
if (Test-Path $auditSection) {
    Remove-Item -Path $auditSection -Recurse -Permanently
    Write-Host "   removed $auditSection"
}

# Step 2: Rename content folder API Keys -> Remoting Clients ---------------

Write-Step "Rename content folder API Keys -> Remoting Clients"
$oldFolder = 'master:/sitecore/system/Modules/PowerShell/Settings/Access/API Keys'
$newFolder = 'master:/sitecore/system/Modules/PowerShell/Settings/Access/Remoting Clients'
if ((Test-Path $oldFolder) -and (-not (Test-Path $newFolder))) {
    Rename-Item -Path $oldFolder -NewName 'Remoting Clients'
    Write-Host "   renamed to $newFolder"
}

# Step 3: Rename/update Context Menu 'Edit Remoting API Key' script --------

Write-Step "Rename 'Edit Remoting API Key' script -> 'Edit Shared Secret Client'"
$editOld = 'master:/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Remoting/Content Editor/Context Menu/Edit Remoting API Key'
$editNew = 'master:/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Remoting/Content Editor/Context Menu/Edit Shared Secret Client'
if ((Test-Path $editOld) -and (-not (Test-Path $editNew))) {
    Rename-Item -Path $editOld -NewName 'Edit Shared Secret Client'
}
if (Test-Path $editNew) {
    $item = Get-Item $editNew
    $script = $item['Script']
    $updated = $script `
        -replace 'Edit Remoting API Key', 'Edit Shared Secret Client' `
        -replace "Modify the API key '", "Modify the Shared Secret Client '" `
        -replace 'Rotating credentials requires updating all clients',
                 'Rotating credentials requires updating all callers of this Shared Secret Client'
    if ($updated -ne $script) {
        $item.Editing.BeginEdit()
        $item['Script'] = $updated
        $item.Editing.EndEdit() > $null
        Write-Host "   script body updated"
    }
}

# Step 4: Rename/update Insert Item 'Remoting API Key' script --------------

Write-Step "Rename 'Insert Item/Remoting API Key' script -> 'Shared Secret Client'"
$insertOld = 'master:/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Remoting/Content Editor/Insert Item/Remoting API Key'
$insertNew = 'master:/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Remoting/Content Editor/Insert Item/Shared Secret Client'
if ((Test-Path $insertOld) -and (-not (Test-Path $insertNew))) {
    Rename-Item -Path $insertOld -NewName 'Shared Secret Client'
}
if (Test-Path $insertNew) {
    $item = Get-Item $insertNew
    $script = $item['Script']
    $updated = $script `
        -replace 'Remoting API Key', 'Shared Secret Client' `
        -replace 'new API key', 'new Shared Secret Client' `
        -replace 'New API Key', 'New Shared Secret Client'
    if ($updated -ne $script) {
        $item.Editing.BeginEdit()
        $item['Script'] = $updated
        $item.Editing.EndEdit() > $null
        Write-Host "   script body updated"
    }
}

Write-Host ""
Write-Host "Cleanup migration complete." -ForegroundColor Green
