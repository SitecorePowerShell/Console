# Unit tests for SPE module helper functions
# Requires TestRunner.ps1 to be dot-sourced first

# ============================================================
# Get-UsingVariables
# ============================================================
Write-Host "`n  [Get-UsingVariables]" -ForegroundColor White

$sb = [scriptblock]::Create('Get-Process')
$result = Get-UsingVariables -ScriptBlock $sb
Assert-Null $result "No `$Using: vars returns null"

$sb = [scriptblock]::Create('Write-Host $Using:myVar')
$result = @(Get-UsingVariables -ScriptBlock $sb)
Assert-Equal $result.Count 1 "Single `$Using: var returns one AST node"
Assert-Like $result[0].SubExpression.VariablePath.UserPath "myVar" "Variable path is 'myVar'"

$sb = [scriptblock]::Create('Write-Host $Using:a $Using:b $Using:a')
$result = @(Get-UsingVariables -ScriptBlock $sb)
Assert-Equal $result.Count 3 "Duplicate `$Using: vars returns all occurrences"

$sb = [scriptblock]::Create('Write-Host $USING:CaseTest')
$result = @(Get-UsingVariables -ScriptBlock $sb)
Assert-Equal $result.Count 1 "Case-insensitive detection of `$USING:"

# ============================================================
# Expand-ScriptSession
# ============================================================
Write-Host "`n  [Expand-ScriptSession]" -ForegroundColor White

$mockSession = [pscustomobject]@{
    Username              = "admin"
    Password              = "b"
    SharedSecret          = "secret123"
    SessionId             = "sess-001"
    Credential            = $null
    UseDefaultCredentials = 1
    Connection            = @(
        [pscustomobject]@{ BaseUri = "https://sc1.example.com" },
        [pscustomobject]@{ BaseUri = "https://sc2.example.com" }
    )
    PersistentSession     = $true
    _HttpClients          = @{}
}

$expanded = Expand-ScriptSession -Session $mockSession

Assert-Equal $expanded.Username "admin" "Username extracted"
Assert-Equal $expanded.Password "b" "Password extracted"
Assert-Equal $expanded.SharedSecret "secret123" "SharedSecret extracted"
Assert-Equal $expanded.SessionId "sess-001" "SessionId extracted"
Assert-True ($expanded.UseDefaultCredentials -is [bool]) "UseDefaultCredentials cast to bool"
Assert-Equal $expanded.UseDefaultCredentials $true "UseDefaultCredentials is true"
Assert-Equal $expanded.ConnectionUri.Count 2 "ConnectionUri maps Connection array"
Assert-Equal $expanded.ConnectionUri[0] "https://sc1.example.com" "First ConnectionUri correct"
Assert-Equal $expanded.ConnectionUri[1] "https://sc2.example.com" "Second ConnectionUri correct"
Assert-True $expanded.PersistentSession "PersistentSession preserved"
Assert-NotNull $expanded.HttpClients "HttpClients mapped from _HttpClients"

# Edge case: empty Connection array
$emptySession = [pscustomobject]@{
    Username = "u"; Password = "p"; SharedSecret = ""; SessionId = ""
    Credential = $null; UseDefaultCredentials = $false
    Connection = @(); PersistentSession = $false; _HttpClients = $null
}
$expanded2 = Expand-ScriptSession -Session $emptySession
Assert-Equal $expanded2.ConnectionUri.Count 0 "Empty Connection produces empty ConnectionUri"

# ============================================================
# Parse-Response
# ============================================================
Write-Host "`n  [Parse-Response]" -ForegroundColor White

# Null/empty response
$result = Parse-Response -Response "" -HasRedirectedMessages $false -Raw $true
Assert-Null $result "Empty response returns null"

$result = Parse-Response -Response $null -HasRedirectedMessages $false -Raw $true
Assert-Null $result "Null response returns null"

# Raw response without messages delimiter
$result = Parse-Response -Response "hello world" -HasRedirectedMessages $false -Raw $true
Assert-Equal $result "hello world" "Raw response without delimiter returns text as-is"

# Raw response with messages delimiter (no actual CLIXML in messages part)
$result = Parse-Response -Response "output text<#messages#>" -HasRedirectedMessages $false -Raw $true
Assert-Equal $result "output text" "Response before delimiter is returned"

# Non-raw empty-ish path -- no messages means ConvertFrom-CliXml called on empty, which returns nothing
$result = Parse-Response -Response "" -HasRedirectedMessages $false -Raw $false
Assert-Null $result "Non-raw empty response returns null"

# ============================================================
# Resolve-UsingVariables (no $Using: vars path)
# ============================================================
Write-Host "`n  [Resolve-UsingVariables - no using vars]" -ForegroundColor White

$sb = [scriptblock]::Create('Get-ChildItem -Path C:\temp')
$result = Resolve-UsingVariables -ScriptBlock $sb -Arguments $null
Assert-Equal $result.ScriptText 'Get-ChildItem -Path C:\temp' "No `$Using: vars returns original script text"
Assert-Null $result.Arguments "Arguments stays null when no using vars and null passed"

$existingArgs = @{ "existing" = "value" }
$result = Resolve-UsingVariables -ScriptBlock $sb -Arguments $existingArgs
Assert-Equal $result.Arguments["existing"] "value" "Existing Arguments preserved when no using vars"

# ============================================================
# Resolve-UsingVariables (with $Using: vars -- run inside module scope)
# ============================================================
Write-Host "`n  [Resolve-UsingVariables - with using vars]" -ForegroundColor White

# These tests must run inside the module scope so Get-UsingVariableValues
# can resolve variables via SessionState. We pass assert functions in.
$speModule = Get-Module SPE
& $speModule {
    param($AssertLike, $AssertTrue, $AssertEqual)

    $testName = "World"
    $sb = [scriptblock]::Create('Write-Host $Using:testName')
    $result = Resolve-UsingVariables -ScriptBlock $sb -Arguments $null
    & $AssertLike $result.ScriptText '*$params.__using_testName*' "Single using var replaced with `$params. prefix"
    & $AssertTrue ($result.Arguments.ContainsKey('__using_testName')) "Arguments contains __using_testName key"
    & $AssertEqual $result.Arguments['__using_testName'] "World" "Argument value resolved from scope"

    # Custom ParamsPrefix
    $testCity = "London"
    $sb2 = [scriptblock]::Create('Write-Host $Using:testCity')
    $result2 = Resolve-UsingVariables -ScriptBlock $sb2 -Arguments $null -ParamsPrefix '$'
    & $AssertLike $result2.ScriptText '*$__using_testCity*' "Custom ParamsPrefix `$ replaces correctly"

    # Multiple using vars including duplicates
    $varA = "alpha"
    $varB = "beta"
    $sb3 = [scriptblock]::Create('Write-Host $Using:varA $Using:varB $Using:varA')
    $result3 = Resolve-UsingVariables -ScriptBlock $sb3 -Arguments $null
    & $AssertTrue (-not $result3.ScriptText.Contains('$Using:')) "All `$Using: references replaced"
    & $AssertTrue ($result3.Arguments.ContainsKey('__using_varA')) "varA in arguments"
    & $AssertTrue ($result3.Arguments.ContainsKey('__using_varB')) "varB in arguments"

    # Preserve existing arguments
    $varX = "x-val"
    $sb4 = [scriptblock]::Create('Write-Host $Using:varX')
    $existingArgs2 = @{ "keep" = "me" }
    $result4 = Resolve-UsingVariables -ScriptBlock $sb4 -Arguments $existingArgs2
    & $AssertEqual $result4.Arguments["keep"] "me" "Existing arguments preserved alongside using vars"
} ${function:Assert-Like} ${function:Assert-True} ${function:Assert-Equal}
