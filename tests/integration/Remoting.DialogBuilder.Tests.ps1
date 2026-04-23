# Integration tests for DialogBuilder extension library
# Requires a running Sitecore instance with SPE.
# Run via: Run-RemotingTests.ps1 -TestFile Remoting.DialogBuilder.Tests.ps1

Write-Host "`n  [DialogBuilder - Setup]" -ForegroundColor White

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $global:sharedSecret -ConnectionUri $protocolHost

# Verify DialogBuilder can be loaded
$loadResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name DialogBuilder
    $db = New-DialogBuilder -Title "Test"
    if ($db._IsDialogBuilder) { "OK" } else { "FAIL" }
} -Raw 2>$null

if ($loadResult -ne "OK") {
    Skip-Test "DialogBuilder integration tests" "Import-Function -Name DialogBuilder failed (not deployed?)"
    Stop-ScriptSession -Session $session
    return
}

# ============================================================
# New-DialogBuilder - builder creation
# ============================================================
Write-Host "`n  [New-DialogBuilder - builder creation]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name DialogBuilder

    $db = New-DialogBuilder -Title "My Dialog" -Description "A test" -Width 600 -Height 500 -OkButtonName "Save" -CancelButtonName "Discard" -ShowHints
    @{
        IsDialogBuilder  = $db._IsDialogBuilder
        Title            = $db._Title
        Description      = $db._Description
        Width            = $db._Width
        Height           = $db._Height
        OkButtonName     = $db._OkButtonName
        CancelButtonName = $db._CancelButtonName
        ShowHints        = $db._ShowHints
        ParameterCount   = $db._Parameters.Count
        ParameterType    = $db._Parameters.GetType().Name
    }
}

Assert-True $result.IsDialogBuilder "_IsDialogBuilder is true"
Assert-Equal $result.Title "My Dialog" "Title is set"
Assert-Equal $result.Description "A test" "Description is set"
Assert-Equal $result.Width 600 "Width is set"
Assert-Equal $result.Height 500 "Height is set"
Assert-Equal $result.OkButtonName "Save" "OkButtonName is set"
Assert-Equal $result.CancelButtonName "Discard" "CancelButtonName is set"
Assert-True $result.ShowHints "ShowHints is true"
Assert-Equal $result.ParameterCount 0 "No parameters initially"
Assert-Equal $result.ParameterType "ArrayList" "_Parameters is ArrayList"

# ============================================================
# Add-DialogField - basic field addition
# ============================================================
Write-Host "`n  [Add-DialogField - basic fields]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name DialogBuilder

    $db = New-DialogBuilder -Title "Test"
    $db | Add-DialogField -Name "field1" -Title "Field One" -Value "hello"
    $db | Add-DialogField -Name "field2" -Title "Field Two" -Value 42

    @{
        ParameterCount = $db._Parameters.Count
        VariableCount  = $db._Variables.Count
        Field1Name     = $db._Parameters[0].Name
        Field1Title    = $db._Parameters[0].Title
        Field1Value    = $db._Parameters[0].Value
        Field2Name     = $db._Parameters[1].Name
        Field2Value    = $db._Parameters[1].Value
        VarField1      = $db._Variables["field1"]
        VarField2      = $db._Variables["field2"]
    }
}

Assert-Equal $result.ParameterCount 2 "Two parameters added"
Assert-Equal $result.VariableCount 2 "Two variables tracked"
Assert-Equal $result.Field1Name "field1" "First field name correct"
Assert-Equal $result.Field1Title "Field One" "First field title correct"
Assert-Equal $result.Field1Value "hello" "First field value correct"
Assert-Equal $result.Field2Name "field2" "Second field name correct"
Assert-Equal $result.Field2Value 42 "Second field value correct"
Assert-Equal $result.VarField1 "hello" "Variable tracks field1"
Assert-Equal $result.VarField2 42 "Variable tracks field2"

# ============================================================
# Add-DialogField - void return (no chaining)
# ============================================================
Write-Host "`n  [Add-DialogField - void return]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name DialogBuilder

    $db = New-DialogBuilder -Title "Test"
    $returnValue = $db | Add-DialogField -Name "f" -Value "v"
    @{
        IsNull = $null -eq $returnValue
    }
}

Assert-True $result.IsNull "Add-DialogField returns null (void)"

# ============================================================
# Type guard - invalid builder rejected
# ============================================================
Write-Host "`n  [Type guard - invalid builder]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name DialogBuilder

    try {
        $fake = @{ Foo = "bar" }
        $fake | Add-DialogField -Name "test" -Value "x"
        "no_error"
    } catch {
        "threw"
    }
} -Raw 2>$null

Assert-Equal $result "threw" "Invalid builder throws on Add-DialogField"

# ============================================================
# Convenience functions - type-specific editors
# ============================================================
Write-Host "`n  [Convenience functions - editors]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name DialogBuilder

    $db = New-DialogBuilder -Title "Test"
    $db | Add-TextField -Name "text1" -Value "hi"
    $db | Add-TextField -Name "pass1" -IsPassword -Value "secret"
    $db | Add-Checkbox -Name "check1"
    $db | Add-InfoText -Name "info1" -Value "Read me"
    $db | Add-MultiLineTextField -Name "multi1" -Lines 5

    @{
        Count       = $db._Parameters.Count
        TextEditor  = $db._Parameters[0].Editor
        PassEditor  = $db._Parameters[1].Editor
        CheckEditor = $db._Parameters[2].Editor
        InfoEditor  = $db._Parameters[3].Editor
        MultiLines  = $db._Parameters[4].Lines
    }
}

Assert-Equal $result.Count 5 "Five fields added"
Assert-True ($null -eq $result.TextEditor -or $result.TextEditor -eq "") "Plain text has no explicit editor (auto)"
Assert-Equal $result.PassEditor "password" "Password field has password editor"
Assert-Equal $result.CheckEditor "checkbox" "Checkbox has checkbox editor"
Assert-Equal $result.InfoEditor "info" "InfoText has info editor"
Assert-Equal $result.MultiLines 5 "MultiLine has Lines=5"

# ============================================================
# Remove-DialogField
# ============================================================
Write-Host "`n  [Remove-DialogField]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name DialogBuilder

    $db = New-DialogBuilder -Title "Test"
    $db | Add-TextField -Name "keep" -Value "a"
    $db | Add-TextField -Name "remove" -Value "b"
    $db | Add-TextField -Name "also_keep" -Value "c"

    $db | Remove-DialogField -Name "remove"

    @{
        ParameterCount = $db._Parameters.Count
        VariableCount  = $db._Variables.Count
        FirstName      = $db._Parameters[0].Name
        SecondName     = $db._Parameters[1].Name
        HasRemoved     = $db._Variables.ContainsKey("remove")
    }
}

Assert-Equal $result.ParameterCount 2 "One field removed, two remain"
Assert-Equal $result.VariableCount 2 "Variable also removed"
Assert-Equal $result.FirstName "keep" "First remaining field is 'keep'"
Assert-Equal $result.SecondName "also_keep" "Second remaining field is 'also_keep'"
Assert-True (-not $result.HasRemoved) "Removed variable no longer tracked"

# ============================================================
# Copy-DialogBuilder - deep copy
# ============================================================
Write-Host "`n  [Copy-DialogBuilder - deep copy]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name DialogBuilder

    $db = New-DialogBuilder -Title "Original"
    $db | Add-TextField -Name "f1" -Value "val1"

    $copy = $db | Copy-DialogBuilder
    $copy | Add-TextField -Name "f2" -Value "val2"

    @{
        OriginalCount    = $db._Parameters.Count
        CopyCount        = $copy._Parameters.Count
        CopyIsBuilder    = $copy._IsDialogBuilder
        CopyTitle        = $copy._Title
        CopyParamType    = $copy._Parameters.GetType().Name
    }
}

Assert-Equal $result.OriginalCount 1 "Original unchanged after copy modified"
Assert-Equal $result.CopyCount 2 "Copy has both fields"
Assert-True $result.CopyIsBuilder "Copy has _IsDialogBuilder"
Assert-Equal $result.CopyTitle "Original" "Copy preserves title"
Assert-Equal $result.CopyParamType "ArrayList" "Copy uses ArrayList"

# ============================================================
# ConvertTo-DialogBuilderJson
# ============================================================
Write-Host "`n  [ConvertTo-DialogBuilderJson]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name DialogBuilder

    $db = New-DialogBuilder -Title "Export Test" -Width 700
    $db | Add-TextField -Name "name" -Value "test"
    $db | Add-Checkbox -Name "flag"

    $json = $db | ConvertTo-DialogBuilderJson
    $parsed = $json | ConvertFrom-Json

    @{
        IsString        = $json -is [string]
        HasTitle        = $parsed.Title -eq "Export Test"
        HasWidth        = $parsed.Width -eq 700
        ParameterCount  = $parsed.Parameters.Count
    }
}

Assert-True $result.IsString "Returns a string"
Assert-True $result.HasTitle "JSON contains Title"
Assert-True $result.HasWidth "JSON contains Width"
Assert-Equal $result.ParameterCount 2 "JSON contains 2 parameters"

# ============================================================
# Test-DialogBuilder - validation passes
# ============================================================
Write-Host "`n  [Test-DialogBuilder - valid dialog]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name DialogBuilder

    $db = New-DialogBuilder -Title "Valid"
    $db | Add-TextField -Name "name" -Value "test"
    $db | Add-Checkbox -Name "flag"

    $valid = $db | Test-DialogBuilder
    @{
        IsValid = $valid
    }
}

Assert-True $result.IsValid "Valid dialog passes Test-DialogBuilder"

# ============================================================
# Test-DialogBuilder - invalid type guard
# ============================================================
Write-Host "`n  [Test-DialogBuilder - invalid builder]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name DialogBuilder

    $fake = @{ Bogus = $true }
    $valid = $fake | Test-DialogBuilder 2>$null
    @{
        IsValid = $valid
    }
}

Assert-Equal $result.IsValid $false "Invalid builder fails Test-DialogBuilder"

# ============================================================
# Field validation - bad name characters
# ============================================================
Write-Host "`n  [Add-DialogField - invalid name rejected]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name DialogBuilder

    $db = New-DialogBuilder -Title "Test"
    try {
        $db | Add-DialogField -Name "bad-name" -Value "x"
        "no_error"
    } catch {
        "threw"
    }
} -Raw 2>$null

Assert-Equal $result "threw" "Invalid field name with hyphen throws"

# ============================================================
# Conditional visibility validation
# ============================================================
Write-Host "`n  [Add-DialogField - conditional visibility validation]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name DialogBuilder

    $db = New-DialogBuilder -Title "Test"
    try {
        $db | Add-DialogField -Name "orphan" -Value "x" -ShowOnValue "yes" -ParentGroupId 0
        "no_error"
    } catch {
        "threw"
    }
} -Raw 2>$null

Assert-Equal $result "threw" "ShowOnValue without ParentGroupId throws"

# ============================================================
# Duplicate field name replacement (H1)
# ============================================================
Write-Host "`n  [Add-DialogField - duplicate name replaces previous]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name DialogBuilder

    $db = New-DialogBuilder -Title "Test"
    $db | Add-TextField -Name "field1" -Value "original"
    $db | Add-TextField -Name "field2" -Value "keep"
    $db | Add-TextField -Name "field1" -Value "replaced"

    @{
        ParameterCount = $db._Parameters.Count
        VariableCount  = $db._Variables.Count
        Field1Value    = $db._Parameters[0].Name
        Field2Value    = $db._Parameters[1].Name
        VarField1      = $db._Variables["field1"]
    }
}

Assert-Equal $result.ParameterCount 2 "Duplicate replaced, two params remain"
Assert-Equal $result.VariableCount 2 "Two variables tracked"
Assert-Equal $result.Field1Value "field2" "First param is now field2 (field1 was removed and re-added)"
Assert-Equal $result.Field2Value "field1" "Second param is the replaced field1"
Assert-Equal $result.VarField1 "replaced" "Variable tracks new value"

# ============================================================
# Visible parameter (H2)
# ============================================================
Write-Host "`n  [Add-DialogField - Visible parameter]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name DialogBuilder

    $db = New-DialogBuilder -Title "Test"
    $db | Add-TextField -Name "shown" -Value "a"
    $db | Add-DialogField -Name "hidden" -Value "context" -Visible $false

    @{
        ShownHasVisible = $db._Parameters[0].ContainsKey("Visible")
        HiddenVisible   = $db._Parameters[1].Visible
    }
}

Assert-True (-not $result.ShownHasVisible) "Visible=true fields do not include Visible key"
Assert-Equal $result.HiddenVisible $false "Hidden field has Visible=false"

# ============================================================
# Per-field Height parameter (H3)
# ============================================================
Write-Host "`n  [Add-DialogField - Height parameter]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name DialogBuilder

    $db = New-DialogBuilder -Title "Test"
    $db | Add-MultiLineTextField -Name "notes" -Lines 5 -Height "200"
    $db | Add-TextField -Name "plain" -Value "x"

    @{
        NotesHeight   = $db._Parameters[0].Height
        PlainHasHeight = $db._Parameters[1].ContainsKey("Height")
    }
}

Assert-Equal $result.NotesHeight "200" "Height set on multi-line field"
Assert-True (-not $result.PlainHasHeight) "Plain text has no Height key"

# ============================================================
# Per-field Validator parameter (H4)
# ============================================================
Write-Host "`n  [Add-DialogField - Validator parameter]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name DialogBuilder

    $db = New-DialogBuilder -Title "Test"
    $db | Add-DialogField -Name "email" -Value "" -Validator { if (-not $value) { $variable.Error = "Required" } }
    $db | Add-TextField -Name "plain" -Value "x"

    @{
        HasValidator      = $null -ne $db._Parameters[0].Validator
        PlainHasValidator = $db._Parameters[1].ContainsKey("Validator")
    }
}

Assert-True $result.HasValidator "Field with Validator has it set"
Assert-True (-not $result.PlainHasValidator) "Plain field has no Validator key"

# ============================================================
# Per-field Description parameter (L2)
# ============================================================
Write-Host "`n  [Add-DialogField - Description parameter]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name DialogBuilder

    $db = New-DialogBuilder -Title "Test"
    $db | Add-DialogField -Name "name" -Value "" -Description "Enter your full name"
    $db | Add-TextField -Name "plain" -Value "x"

    @{
        HasDescription      = $db._Parameters[0].Description
        PlainHasDescription = $db._Parameters[1].ContainsKey("Description")
    }
}

Assert-Equal $result.HasDescription "Enter your full name" "Description is set on field"
Assert-True (-not $result.PlainHasDescription) "Plain field has no Description key"

# ============================================================
# DateTimePicker null default (L1)
# ============================================================
Write-Host "`n  [Add-DateTimePicker - null default value]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name DialogBuilder

    $db = New-DialogBuilder -Title "Test"
    $db | Add-DateTimePicker -Name "empty_date"
    $db | Add-DateTimePicker -Name "set_date" -Value ([DateTime]::Parse("2026-01-15"))

    @{
        EmptyValue = $db._Parameters[0].Value
        SetValue   = $db._Parameters[1].Value
        IsNull     = $null -eq $db._Parameters[0].Value
    }
}

Assert-True $result.IsNull "Empty DateTimePicker has null value"
Assert-True ($null -ne $result.SetValue) "Set DateTimePicker has non-null value"

# ============================================================
# Test-DialogBuilder - item-picker Value type validation
# ============================================================
Write-Host "`n  [Test-DialogBuilder - item-picker Value type]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name DialogBuilder

    $homeItem = Get-Item -Path "master:/sitecore/content/Home"

    $okBuilder = New-DialogBuilder -Title "OK"
    $okBuilder | Add-Droplink -Name "item" -Source "/sitecore/content" -Value $homeItem
    $okValid = $okBuilder | Test-DialogBuilder 2>$null

    # Force a non-Item Value onto a droplink to simulate accidental misuse
    $badBuilder = New-DialogBuilder -Title "Bad"
    $badBuilder | Add-Droplink -Name "item" -Source "/sitecore/content" -Value $homeItem
    $badBuilder._Parameters[0].Value = $homeItem.ID.ToString()
    $badValid = $badBuilder | Test-DialogBuilder 2>$null

    # Multi-item editor with a mixed array
    $badMulti = New-DialogBuilder -Title "BadMulti"
    $badMulti | Add-DialogField -Name "items" -Editor "treelist" -Value @($homeItem, "not-an-item")
    $multiValid = $badMulti | Test-DialogBuilder 2>$null

    @{
        OkValid    = $okValid
        BadValid   = $badValid
        MultiValid = $multiValid
    }
}

Assert-True $result.OkValid "Droplink with Item Value passes validation"
Assert-Equal $result.BadValid $false "Droplink with string Value fails validation"
Assert-Equal $result.MultiValid $false "Treelist with non-Item element fails validation"

# ============================================================
# Null Value propagation through item-picker wrappers
# ============================================================
Write-Host "`n  [Add-Dropl* - null Value propagates (no auto-init)]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name DialogBuilder

    $db = New-DialogBuilder -Title "NullValue"
    $db | Add-Droplink        -Name "dlink"  -Source "DataSource=/sitecore/content" -Value $null
    $db | Add-Droplist        -Name "dlist"  -Source "DataSource=/sitecore/content" -Value $null
    $db | Add-Droptree        -Name "dtree"  -Source "DataSource=/sitecore/content" -Value $null
    $db | Add-GroupedDroplink -Name "gdlink" -Source "DataSource=/sitecore/content" -Value $null
    $db | Add-GroupedDroplist -Name "gdlist" -Source "DataSource=/sitecore/content" -Value $null

    @{
        DroplinkValue        = $db._Parameters[0].Value
        DroplistValue        = $db._Parameters[1].Value
        DroptreeValue        = $db._Parameters[2].Value
        GroupedDroplinkValue = $db._Parameters[3].Value
        GroupedDroplistValue = $db._Parameters[4].Value
    }
}

Assert-True ($null -eq $result.DroplinkValue)        "Add-Droplink -Value `$null propagates null"
Assert-True ($null -eq $result.DroplistValue)        "Add-Droplist -Value `$null propagates null"
Assert-True ($null -eq $result.DroptreeValue)        "Add-Droptree -Value `$null propagates null"
Assert-True ($null -eq $result.GroupedDroplinkValue) "Add-GroupedDroplink -Value `$null propagates null"
Assert-True ($null -eq $result.GroupedDroplistValue) "Add-GroupedDroplist -Value `$null propagates null"

# ============================================================
# -AllowNone switch appends "allownone" to editor string
# ============================================================
Write-Host "`n  [Add-Dropl* - AllowNone appends editor suffix]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name DialogBuilder

    $db = New-DialogBuilder -Title "AllowNone"
    $db | Add-Droplink        -Name "dlink"  -Source "DataSource=/sitecore/content" -Value $null -AllowNone
    $db | Add-Droplist        -Name "dlist"  -Source "DataSource=/sitecore/content" -Value $null -AllowNone
    $db | Add-Droptree        -Name "dtree"  -Source "DataSource=/sitecore/content" -Value $null -AllowNone
    $db | Add-GroupedDroplink -Name "gdlink" -Source "DataSource=/sitecore/content" -Value $null -AllowNone
    $db | Add-GroupedDroplist -Name "gdlist" -Source "DataSource=/sitecore/content" -Value $null -AllowNone
    $db | Add-ItemPicker      -Name "item"   -Value $null -AllowNone

    @{
        DroplinkEditor        = $db._Parameters[0].Editor
        DroplistEditor        = $db._Parameters[1].Editor
        DroptreeEditor        = $db._Parameters[2].Editor
        GroupedDroplinkEditor = $db._Parameters[3].Editor
        GroupedDroplistEditor = $db._Parameters[4].Editor
        ItemEditor            = $db._Parameters[5].Editor
    }
}

Assert-Equal $result.DroplinkEditor        "droplink allownone"        "Add-Droplink -AllowNone emits 'droplink allownone'"
Assert-Equal $result.DroplistEditor        "droplist allownone"        "Add-Droplist -AllowNone emits 'droplist allownone'"
Assert-Equal $result.DroptreeEditor        "droptree allownone"        "Add-Droptree -AllowNone emits 'droptree allownone'"
Assert-Equal $result.GroupedDroplinkEditor "groupeddroplink allownone" "Add-GroupedDroplink -AllowNone emits suffix"
Assert-Equal $result.GroupedDroplistEditor "groupeddroplist allownone" "Add-GroupedDroplist -AllowNone emits suffix"
Assert-Equal $result.ItemEditor            "item allownone"            "Add-Item -AllowNone emits 'item allownone'"

# ============================================================
# -Placeholder stores value on parameter hashtable
# ============================================================
Write-Host "`n  [Add-Dropl* - Placeholder attaches to parameter]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name DialogBuilder

    $db = New-DialogBuilder -Title "Placeholder"
    $db | Add-Droplist -Name "layout" -Source "DataSource=/sitecore/layout" -Value $null -AllowNone -Placeholder "Select a layout..."
    $db | Add-Droplist -Name "nolabel" -Source "DataSource=/sitecore/layout" -Value $null -AllowNone

    @{
        WithLabel       = $db._Parameters[0].Placeholder
        WithoutLabelKey = $db._Parameters[1].ContainsKey("Placeholder")
    }
}

Assert-Equal $result.WithLabel "Select a layout..." "Placeholder text stored on parameter hashtable"
Assert-True (-not $result.WithoutLabelKey) "Omitted Placeholder not added as empty string"

# ============================================================
# Test-DialogBuilder accepts null Value on item-picker editors
# ============================================================
Write-Host "`n  [Test-DialogBuilder - null Value on item-picker accepted]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name DialogBuilder

    $db = New-DialogBuilder -Title "NullTolerated"
    $db | Add-Droplink -Name "opt" -Source "DataSource=/sitecore/content" -Value $null -AllowNone
    $nullValid = $db | Test-DialogBuilder 2>$null
    @{
        Valid = $nullValid
    }
}

Assert-True $result.Valid "Test-DialogBuilder passes on null Value with AllowNone"

# ============================================================
# Cleanup
# ============================================================
Stop-ScriptSession -Session $session
