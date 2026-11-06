# Remoting Tests - Media Upload security
# Path traversal normalization, double-prefix guard, unauthenticated upload rejection.

Write-Host "`n  [Media Upload - security]" -ForegroundColor White

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost
$localFilePath = (Resolve-Path (Join-Path $PSScriptRoot "..\fixtures")).Path

try {
    # ---- Path traversal in Destination is neutralised by MediaLibraryPath prefixing ----
    # ProcessMediaUpload prepends /sitecore/media library when the incoming path
    # doesn't already start with it, so "../../system/foo" becomes a literal subpath
    # under the media library, not an escape.
    Get-Item -Path "$($localFilePath)\kitten.jpg" |
        Send-RemoteItem -Session $session -RootPath Media -Destination "../../system/spe-security/traversal.jpg" `
            -ErrorAction SilentlyContinue

    $outsideMediaExists = Invoke-RemoteScript -Session $session -ScriptBlock {
        Test-Path -Path "master:\system\spe-security\traversal" -ErrorAction SilentlyContinue
    }
    Assert-Equal $outsideMediaExists $false "Path traversal: item not created under /sitecore/system"

    $templatesEscape = Invoke-RemoteScript -Session $session -ScriptBlock {
        Test-Path -Path "master:\templates\spe-security\traversal" -ErrorAction SilentlyContinue
    }
    Assert-Equal $templatesEscape $false "Path traversal: item not created under /sitecore/templates"

    # ---- Double-prefix guard: Destination that already starts with media library path ----
    Get-Item -Path "$($localFilePath)\kitten.jpg" |
        Send-RemoteItem -Session $session -RootPath Media `
            -Destination "/sitecore/media library/Images/spe-security/prefixed.jpg" `
            -ErrorAction SilentlyContinue
    $prefixed = Invoke-RemoteScript -Session $session -ScriptBlock {
        Test-Path -Path "master:\media library\images\spe-security\prefixed"
    }
    $doublePrefixed = Invoke-RemoteScript -Session $session -ScriptBlock {
        Test-Path -Path "master:\media library\sitecore\media library\images\spe-security\prefixed" -ErrorAction SilentlyContinue
    }
    Assert-Equal $prefixed $true "Double-prefix guard: item lands at single media library path"
    Assert-Equal $doublePrefixed $false "Double-prefix guard: no doubled prefix path is created"

    # ---- Unauthenticated upload rejection ----
    # A wrong shared secret should produce a 401 at the upload endpoint. We construct
    # a session with a deliberately bad secret and expect an error surface.
    $badSecret = "00000000000000000000000000000000-wrong-secret-for-negative-test"
    $badSession = New-ScriptSession -Username "sitecore\admin" -SharedSecret $badSecret -ConnectionUri $protocolHost

    $authError = $null
    Get-Item -Path "$($localFilePath)\kitten.jpg" |
        Send-RemoteItem -Session $badSession -RootPath Media -Destination "Images/spe-security/unauth.jpg" `
            -ErrorVariable authError -ErrorAction SilentlyContinue

    $unauthCreated = Invoke-RemoteScript -Session $session -ScriptBlock {
        Test-Path -Path "master:\media library\images\spe-security\unauth" -ErrorAction SilentlyContinue
    }
    Assert-True ($authError.Count -gt 0 -or $unauthCreated -eq $false) `
        "Unauthenticated upload: either surfaces an error or refuses to create the item (errs=$($authError.Count) created=$unauthCreated)"
    Assert-Equal $unauthCreated $false "Unauthenticated upload: no media item created with bad shared secret"

    Stop-ScriptSession -Session $badSession -ErrorAction SilentlyContinue
}
finally {
    Invoke-RemoteScript -Session $session -ScriptBlock {
        foreach ($p in @(
            "master:\media library\images\spe-security\",
            "master:\media library\sitecore\"
        )) {
            Remove-Item -Path $p -Recurse -ErrorAction SilentlyContinue
        }
    } -ErrorAction SilentlyContinue
    Stop-ScriptSession -Session $session
}
