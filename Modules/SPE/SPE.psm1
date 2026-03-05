function Expand-ScriptSession {
    param([pscustomobject]$Session)
    [pscustomobject]@{
        Username              = $Session.Username
        Password              = $Session.Password
        SharedSecret          = $Session.SharedSecret
        SessionId             = $Session.SessionId
        Credential            = $Session.Credential
        UseDefaultCredentials = [bool]$Session.UseDefaultCredentials
        ConnectionUri         = @($Session.Connection | ForEach-Object { $_.BaseUri })
        PersistentSession     = $Session.PersistentSession
        HttpClients           = $Session._HttpClients
    }
}

function New-SpeHttpClient {
    param(
        [string]$Username,
        [string]$Password,
        [string]$SharedSecret,
        [System.Management.Automation.PSCredential]$Credential,
        [bool]$UseDefaultCredentials,
        [Uri]$Uri,
        [hashtable]$Cache
    )
    $cacheKey = $Uri.AbsoluteUri
    if ($null -ne $Cache -and $Cache.ContainsKey($cacheKey)) {
        $client = $Cache[$cacheKey]
    } else {
        $handler = New-Object System.Net.Http.HttpClientHandler
        $handler.AutomaticDecompression = [System.Net.DecompressionMethods]::GZip -bor [System.Net.DecompressionMethods]::Deflate
        if ($Credential) { $handler.Credentials = $Credential }
        if ($UseDefaultCredentials) { $handler.UseDefaultCredentials = $true }
        $client = New-Object System.Net.Http.HttpClient $handler
        if ($null -ne $Cache) { $Cache[$cacheKey] = $client }
    }
    # Auth header is refreshed on every call; JWT tokens expire after 30 seconds
    if (![string]::IsNullOrEmpty($SharedSecret)) {
        $token = New-Jwt -Algorithm 'HS256' -Issuer 'SPE Remoting' -Audience ($Uri.GetLeftPart([System.UriPartial]::Authority)) -Name $Username -SecretKey $SharedSecret -ValidforSeconds 30
        $client.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)
    } else {
        $authBytes = [System.Text.Encoding]::GetEncoding("iso-8859-1").GetBytes("${Username}:${Password}")
        $client.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Basic", [System.Convert]::ToBase64String($authBytes))
    }
    $client
}

function Resolve-UsingVariables {
    <#
        .SYNOPSIS
            Detects $Using: variable references in a scriptblock, resolves their values from
            the caller's scope, and returns substituted script text plus a populated Arguments
            hashtable ready to pass to the remote endpoint.
    #>
    param(
        [scriptblock]$ScriptBlock,
        [hashtable]$Arguments,
        [string]$ParamsPrefix = '$params.'
    )

    $usingVariables = @(Get-UsingVariables -ScriptBlock $ScriptBlock |
        Group-Object -Property SubExpression |
        ForEach-Object { $_.Group | Select-Object -First 1 })

    if ($usingVariables.Count -eq 0) {
        return [pscustomobject]@{ ScriptText = $ScriptBlock.ToString(); Arguments = $Arguments }
    }

    if (!$Arguments) { $Arguments = @{} }

    $usingVarValues = Get-UsingVariableValues -UsingVar $usingVariables

    # Build a lookup from variable user-path to the replacement parameter name
    $replacementMap = @{}
    foreach ($usingVarValue in $usingVarValues) {
        $Arguments[$usingVarValue.NewName.TrimStart('$')] = $usingVarValue.Value
        $userPath = $usingVarValue.NewVarName -replace '^__using_', ''
        $replacementMap[$userPath] = "$($ParamsPrefix)$($usingVarValue.NewName.TrimStart('$'))"
    }

    # Replace using AST extents (reverse offset order) to avoid shifting positions
    $command = $ScriptBlock.ToString()
    # AST offsets are absolute; ToString() returns the body without braces for literal scriptblocks.
    # For [scriptblock]::Create() scriptblocks there are no braces, so no +1 adjustment needed.
    $baseOffset = $ScriptBlock.Ast.Extent.StartOffset
    if ($ScriptBlock.Ast.Extent.Text.StartsWith('{')) { $baseOffset++ }
    $allNodes = $ScriptBlock.Ast.FindAll(
        { $args[0] -is [System.Management.Automation.Language.UsingExpressionAst] }, $true) |
        Sort-Object { $_.Extent.StartOffset } -Descending
    foreach ($node in $allNodes) {
        $userPath = $node.SubExpression.VariablePath.UserPath
        if ($replacementMap.ContainsKey($userPath)) {
            $start = $node.Extent.StartOffset - $baseOffset
            $end   = $node.Extent.EndOffset - $baseOffset
            $command = $command.Substring(0, $start) + $replacementMap[$userPath] + $command.Substring($end)
        }
    }

    [pscustomobject]@{ ScriptText = $command; Arguments = $Arguments }
}

function Get-UsingVariables {
    param ([scriptblock]$ScriptBlock)
    if($ScriptBlock.ToString().IndexOf("`$using:", [System.StringComparison]::OrdinalIgnoreCase) -eq -1) { return }
    $ScriptBlock.Ast.FindAll({$args[0] -is [System.Management.Automation.Language.UsingExpressionAst]},$true)
}

function Get-UsingVariableValues {
    param ([System.Management.Automation.Language.UsingExpressionAst[]]$usingVar)
    ForEach ($var in $usingVar) {
        try {
            $value = $null
            $isRunspace = ($MyInvocation.CommandOrigin -eq [System.Management.Automation.CommandOrigin]::Runspace -or $MyInvocation.CommandOrigin -eq [System.Management.Automation.CommandOrigin]::Internal)
            $userpath = $Var.SubExpression.VariablePath.UserPath
            if ($isRunspace -and (Test-Path -Path "variable:\$($userpath)")) {
                Write-Verbose "Checking the Runspace for the variable $($userpath)."
                $value = Get-Variable -Name $userpath
            }

            if($value -eq $null -or [string]::IsNullOrEmpty($value.Value)) {
                Write-Verbose "Checking the SessionState for the variable $($userpath)."
                $value = ($PSCmdlet.SessionState.PSVariable.Get($userpath))
                if ([string]::IsNullOrEmpty($value)) {
                    throw 'No value!'
                }
            }
            [pscustomobject]@{
                Name = $var.SubExpression.Extent.Text
                Value = $value.Value
                NewName = ('$__using_{0}' -f $var.SubExpression.VariablePath.UserPath)
                NewVarName = ('__using_{0}' -f $var.SubExpression.VariablePath.UserPath)
            }
        } catch {
            throw "The value of the using variable '$($var.SubExpression.Extent.Text)' cannot be retrieved because it has not been set in the local session."
        }
    }
}

