<#
.SYNOPSIS
    One-shot migration to the Remoting Clients v1 template hierarchy.

.DESCRIPTION
    Introduces the base Remoting Client template and the OAuth Client
    template, and renames the existing Remoting API Key template to
    Shared Secret Client while lifting common fields (Enabled, Expires,
    Impersonated User, Policy, Request Limit, Throttle Window, Throttle
    Action) up to the new base.

    Runs idempotently: every step checks whether the target state is
    already in place.

    Runs inside a remoting session via Invoke-RemoteScript against the
    Sitecore container. After running, a 'task pull' re-serializes the
    changes to YAML.
#>
$ErrorActionPreference = 'Stop'

# Constants ----------------------------------------------------------------

$templatesRoot           = 'master:/sitecore/templates/Modules/PowerShell Console/Remoting'
$legacyClientTemplateId  = '{55AB1AA8-890E-401E-AF06-094CA21E0E2D}'   # existing Remoting API Key template
$legacyFolderTemplateId  = '{B2E45D7F-A89C-4E1D-9B3A-7C5E2F4A6B8D}'   # placeholder - resolved by path below
$standardTemplateId      = '{1930BBEB-7805-471A-A3BE-4858AC7CF696}'
$templateTemplateId      = '{AB86861A-6030-46C5-B394-E8F99E8B87DB}'
$templateSectionId       = '{E269FBB5-3750-427A-9149-7AA950B49301}'
$templateFieldId         = '{455A3E98-A627-4B40-8035-E683A0331AC7}'

function Write-Step {
    param([string]$Message)
    Write-Host "==> $Message" -ForegroundColor Cyan
}

# Step 1: Base Remoting Client template ------------------------------------

Write-Step "Create base 'Remoting Client' template"
$basePath = "$templatesRoot/Remoting Client"
if (Test-Path $basePath) {
    Write-Host "   (already exists)"
} else {
    $baseTemplate = New-Item -Path $templatesRoot -Name 'Remoting Client' -ItemType $templateTemplateId
    $baseTemplate."__Icon"          = 'office/32x32/address_group.png'
    $baseTemplate."__Base template" = $standardTemplateId
}
$baseTemplate = Get-Item -Path $basePath
$baseId = $baseTemplate.ID.ToString()

# Step 2: Sections on base -------------------------------------------------

Write-Step "Create sections on base template (Runtime, State, Throttling, Audit)"
foreach ($sectionName in @('Runtime', 'State', 'Throttling', 'Audit')) {
    $sectionPath = "$basePath/$sectionName"
    if (-not (Test-Path $sectionPath)) {
        New-Item -Path $basePath -Name $sectionName -ItemType $templateSectionId | Out-Null
    }
}

# Step 3: Move existing fields up to base ----------------------------------

Write-Step "Move common fields from Remoting API Key template up to base"
$legacyPath = "$templatesRoot/Remoting API Key"
$fieldMoves = [ordered]@{
    "$legacyPath/Authentication/Enabled"          = "$basePath/State"
    "$legacyPath/Authentication/Expires"          = "$basePath/State"
    "$legacyPath/Authorization/Impersonate User"  = "$basePath/Runtime"
    "$legacyPath/Authorization/Policy"            = "$basePath/Runtime"
    "$legacyPath/Throttling/Request Limit"        = "$basePath/Throttling"
    "$legacyPath/Throttling/Throttle Action"      = "$basePath/Throttling"
    "$legacyPath/Throttling/Throttle Window"      = "$basePath/Throttling"
}
foreach ($kv in $fieldMoves.GetEnumerator()) {
    $src  = $kv.Key
    $dest = $kv.Value
    if (Test-Path $src) {
        Move-Item -Path $src -Destination $dest
        Write-Host "   moved $($src.Split('/')[-1]) -> $dest"
    }
}

# Step 4: Remove now-empty Authorization and Throttling sections on legacy --

Write-Step "Remove now-empty sections on legacy template"
foreach ($section in @("$legacyPath/Authorization", "$legacyPath/Throttling")) {
    if (Test-Path $section) {
        $sectionItem = Get-Item $section
        if (-not $sectionItem.HasChildren) {
            Remove-Item -Path $section -Recurse -Permanently
            Write-Host "   removed $section"
        }
    }
}

# Step 5: Create Audit fields on base --------------------------------------

Write-Step "Create Audit section fields (Last Used At / Via / Client)"
$auditFields = @(
    @{ Name = 'Last Used At';     Type = 'Datetime' },
    @{ Name = 'Last Used Via';    Type = 'Single-Line Text' },
    @{ Name = 'Last Used Client'; Type = 'Single-Line Text' }
)
foreach ($f in $auditFields) {
    $fieldPath = "$basePath/Audit/$($f.Name)"
    if (-not (Test-Path $fieldPath)) {
        $fieldItem = New-Item -Path "$basePath/Audit" -Name $f.Name -ItemType $templateFieldId
        $fieldItem.Type = $f.Type
    }
}

# Step 6: Rename Remoting API Key template -> Shared Secret Client ---------

Write-Step "Rename 'Remoting API Key' template -> 'Shared Secret Client'"
$sharedSecretPath = "$templatesRoot/Shared Secret Client"
if ((Test-Path $legacyPath) -and (-not (Test-Path $sharedSecretPath))) {
    Rename-Item -Path $legacyPath -NewName 'Shared Secret Client'
}
$sharedSecretItem = Get-Item -Path $sharedSecretPath
$sharedSecretItem."__Base template" = "$standardTemplateId|$baseId"

# Step 7: OAuth Client template --------------------------------------------

Write-Step "Create 'OAuth Client' template"
$oauthPath = "$templatesRoot/OAuth Client"
if (-not (Test-Path $oauthPath)) {
    $oauthTemplate = New-Item -Path $templatesRoot -Name 'OAuth Client' -ItemType $templateTemplateId
    $oauthTemplate."__Icon"          = 'office/32x32/certificate.png'
    $oauthTemplate."__Base template" = "$standardTemplateId|$baseId"

    New-Item -Path $oauthPath -Name 'Authentication' -ItemType $templateSectionId | Out-Null

    $issuerField = New-Item -Path "$oauthPath/Authentication" -Name 'Allowed Issuer' -ItemType $templateFieldId
    $issuerField.Type = 'Single-Line Text'

    $clientIdsField = New-Item -Path "$oauthPath/Authentication" -Name 'OAuth Client Ids' -ItemType $templateFieldId
    $clientIdsField.Type = 'Multi-Line Text'
}

# Step 8: Rename folder template -------------------------------------------

Write-Step "Rename 'Remoting API Keys Folder' template -> 'Remoting Clients Folder'"
$oldFolderTplPath = "$templatesRoot/Remoting API Keys Folder"
$newFolderTplPath = "$templatesRoot/Remoting Clients Folder"
if ((Test-Path $oldFolderTplPath) -and (-not (Test-Path $newFolderTplPath))) {
    Rename-Item -Path $oldFolderTplPath -NewName 'Remoting Clients Folder'
}

# Step 9: Insert Options on the folder template Standard Values ------------

Write-Step "Update folder template's Insert Options to the two new client templates"
$folderStdValues = "$newFolderTplPath/__Standard Values"
if (Test-Path $folderStdValues) {
    $stdValuesItem = Get-Item $folderStdValues
    $oauthItem  = Get-Item $oauthPath
    $sharedItem = Get-Item $sharedSecretPath
    $stdValuesItem."__Masters" = "$($sharedItem.ID)|$($oauthItem.ID)"
}

Write-Host ""
Write-Host "Migration complete." -ForegroundColor Green
Write-Host "Base template:       $($baseTemplate.Paths.FullPath) ($baseId)"
Write-Host "Shared Secret Client: $sharedSecretPath"
Write-Host "OAuth Client:        $oauthPath"
