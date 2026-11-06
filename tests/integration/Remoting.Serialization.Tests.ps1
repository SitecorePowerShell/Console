# Remoting Tests - Serialization (Export/Import-Item, Export/Import-User, Export/Import-Role)
# Validates the VersionSpecific abstraction layer for deprecated serialization APIs.

Write-Host "`n  [Serialization]" -ForegroundColor White

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

# ---------- Export-Item / Import-Item ----------

# Create a test item to serialize
$testItemPath = "master:\content\Home\SpeSerializationTest"
Invoke-RemoteScript -Session $session -ScriptBlock {
    if (Test-Path -Path $using:testItemPath) {
        Remove-Item -Path $using:testItemPath -Recurse -Force
    }
    New-Item -Path "master:\content\Home" -Name "SpeSerializationTest" -ItemType "Common/Folder" | Out-Null
    New-Item -Path $using:testItemPath -Name "ChildItem" -ItemType "Common/Folder" | Out-Null
}

# Export-Item to default path
$exportResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $item = Get-Item -Path $using:testItemPath
    Export-Item -Item $item
    $reference = $item.Database.Name + $item.Paths.Path
    $filePath = [Sitecore.Data.Serialization.PathUtils]::GetFilePath($reference)
    Test-Path -Path $filePath
}
Assert-Equal $exportResult $true "Export-Item serializes item to default path"

# Export-Item with -Recurse
$exportRecurseResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $item = Get-Item -Path $using:testItemPath
    Export-Item -Item $item -Recurse
    $reference = $item.Database.Name + $item.Paths.Path
    $dirPath = [Sitecore.Data.Serialization.PathUtils]::GetDirectoryPath($reference)
    $childFiles = if (Test-Path $dirPath) { Get-ChildItem -Path $dirPath -Recurse -Filter "*.item" } else { @() }
    $childFiles.Count -ge 1
}
Assert-True $exportRecurseResult "Export-Item -Recurse serializes child items"

# Delete the item, then Import-Item to restore it from the serialized file path
$importResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $item = Get-Item -Path $using:testItemPath
    $reference = $item.Database.Name + $item.Paths.Path
    $filePath = [Sitecore.Data.Serialization.PathUtils]::GetFilePath($reference)
    $dirPath = [Sitecore.Data.Serialization.PathUtils]::GetDirectoryPath($reference)

    Remove-Item -Path $using:testItemPath -Recurse -Force
    $exists = Test-Path -Path $using:testItemPath
    if ($exists) { return "item still exists after delete" }

    # Import the parent item file, then import the child tree
    Import-Item -Path $filePath
    Import-Item -Path $dirPath -Recurse
    Test-Path -Path $using:testItemPath
}
Assert-Equal $importResult $true "Import-Item restores item from serialized file"

# Cleanup: remove test item and serialized files
Invoke-RemoteScript -Session $session -ScriptBlock {
    if (Test-Path -Path $using:testItemPath) {
        Remove-Item -Path $using:testItemPath -Recurse -Force
    }
    $reference = "master" + "/sitecore/content/Home/SpeSerializationTest"
    $filePath = [Sitecore.Data.Serialization.PathUtils]::GetFilePath($reference)
    $dirPath = [Sitecore.Data.Serialization.PathUtils]::GetDirectoryPath($reference)
    if (Test-Path $filePath) { Remove-Item $filePath -Force }
    if (Test-Path $dirPath) { Remove-Item $dirPath -Recurse -Force }
}

# ---------- Export-Role / Import-Role ----------

$testRoleName = "sitecore\SpeSerializationTestRole"

# Create a test role
Invoke-RemoteScript -Session $session -ScriptBlock {
    if (Get-Role -Identity $using:testRoleName -ErrorAction SilentlyContinue) {
        Remove-Role -Identity $using:testRoleName -ErrorAction SilentlyContinue
    }
    New-Role -Identity $using:testRoleName
}

# Export-Role returns the output file path
$exportRoleResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $outputPath = Export-Role -Identity $using:testRoleName
    if ($outputPath -and (Test-Path -Path $outputPath)) { $true } else { $false }
}
Assert-Equal $exportRoleResult $true "Export-Role serializes role to file"

# Delete role, then Import-Role to restore
$importRoleResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    Remove-Role -Identity $using:testRoleName -ErrorAction SilentlyContinue
    $exists = Get-Role -Identity $using:testRoleName -ErrorAction SilentlyContinue
    if ($exists) { return "role still exists after delete" }

    Import-Role -Identity $using:testRoleName
    $restored = Get-Role -Identity $using:testRoleName -ErrorAction SilentlyContinue
    $null -ne $restored
}
Assert-Equal $importRoleResult $true "Import-Role restores role from serialized file"

# Cleanup role and serialized file
Invoke-RemoteScript -Session $session -ScriptBlock {
    # Get the serialized file path before removing the role
    $root = [Sitecore.Data.Serialization.PathUtils]::Root + "security\"
    $parts = $using:testRoleName -split "\\"
    $domain = $parts[0]
    $account = $parts[1]
    $ext = [Sitecore.Data.Serialization.PathUtils]::RoleExtension
    $rolePath = $root + $domain + "\Roles\" + $account + $ext
    if (Test-Path $rolePath) { Remove-Item $rolePath -Force }
    Remove-Role -Identity $using:testRoleName -ErrorAction SilentlyContinue
}

# ---------- Export-User / Import-User ----------

$testUserName = "sitecore\SpeSerializationTestUser"

# Create a test user
Invoke-RemoteScript -Session $session -ScriptBlock {
    if (Get-User -Identity $using:testUserName -ErrorAction SilentlyContinue) {
        Remove-User -Identity $using:testUserName -ErrorAction SilentlyContinue
    }
    New-User -Identity $using:testUserName -Password "Test12345!" -Enabled
}

# Export-User returns the output file path
$exportUserResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $outputPath = Export-User -Identity $using:testUserName
    if ($outputPath -and (Test-Path -Path $outputPath)) { $true } else { $false }
}
Assert-Equal $exportUserResult $true "Export-User serializes user to file"

# Delete user, then Import-User to restore
$importUserResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    Remove-User -Identity $using:testUserName -ErrorAction SilentlyContinue
    $exists = Get-User -Identity $using:testUserName -ErrorAction SilentlyContinue
    if ($exists) { return "user still exists after delete" }

    Import-User -Identity $using:testUserName
    $restored = Get-User -Identity $using:testUserName -ErrorAction SilentlyContinue
    $null -ne $restored
}
Assert-Equal $importUserResult $true "Import-User restores user from serialized file"

# Cleanup user and serialized file
Invoke-RemoteScript -Session $session -ScriptBlock {
    $root = [Sitecore.Data.Serialization.PathUtils]::Root + "security\"
    $parts = $using:testUserName -split "\\"
    $domain = $parts[0]
    $account = $parts[1]
    $ext = [Sitecore.Data.Serialization.PathUtils]::UserExtension
    $userPath = $root + $domain + "\Users\" + $account + $ext
    if (Test-Path $userPath) { Remove-Item $userPath -Force }
    Remove-User -Identity $using:testUserName -ErrorAction SilentlyContinue
}

Stop-ScriptSession -Session $session
