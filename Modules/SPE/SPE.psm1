function Get-UsingVariables {
    param ([scriptblock]$ScriptBlock)
    if($ScriptBlock.ToString().IndexOf("`$using:", [System.StringComparison]::OrdinalIgnoreCase) -eq -1) { return }
    $ScriptBlock.Ast.FindAll({$args[0] -is [System.Management.Automation.Language.UsingExpressionAst]},$true)
}

function Get-UsingVariableValues {
    param ([System.Management.Automation.Language.UsingExpressionAst[]]$usingVar)
    $usingVar = $usingVar | Group SubExpression | ForEach {$_.Group | Select -First 1}        
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

function Convert-UsingScript {
    Param (
        [scriptblock]$ScriptBlock
    )
    
    $usingVariables = @(Get-UsingVariables -ScriptBlock $ScriptBlock)
    $list = New-Object 'System.Collections.Generic.List`1[System.Management.Automation.Language.VariableExpressionAst]'
    $params = New-Object System.Collections.ArrayList
    
    if ($script:Add_) {
        $params.Add('$_') | Out-Null
    }

    if ($usingVariables) {        
        foreach ($ast in $usingVariables) {
            $list.Add($ast.SubExpression) | Out-Null
        }

        $usingVariableData = @(Get-UsingVariableValues $usingVariables)
        $params.AddRange(@($usingVariableData.NewName | Select -Unique)) | Out-Null
    } 
    
    $newParams = $params -join ', '
    $tuple=[Tuple]::Create($list, $newParams)
    $bindingFlags = [Reflection.BindingFlags]"Default,NonPublic,Instance"

    $getWithInputHandlingForInvokeCommandImpl = ($scriptBlock.ast.gettype().GetMethod('GetWithInputHandlingForInvokeCommandImpl',$bindingFlags))
    $stringScriptBlock = $getWithInputHandlingForInvokeCommandImpl.Invoke($scriptBlock.ast,@($tuple))
    if ([scriptblock]::Create($stringScriptBlock).Ast.endblock[0].statements.extent.text.startswith('$input |')) {
        $stringScriptBlock = $stringScriptBlock -replace '\$Input \|'
    }
    if (-NOT $scriptBlock.Ast.ParamBlock) {
        $stringScriptBlock = "param($($newParams))`n$($stringScriptBlock)"
        [scriptblock]::Create($stringScriptBlock)
    } else {
        [scriptblock]::Create($stringScriptBlock)
    }
}

function New-UsingBlock {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [AllowEmptyCollection()]
        [AllowNull()]
        [Object]
        $InputObject,
 
        [Parameter(Mandatory = $true)]
        [scriptblock]
        $ScriptBlock
    )
 
    try {
        . $ScriptBlock
    } finally {
        if ($null -ne $InputObject -and $InputObject -is [System.IDisposable])
        {
            $InputObject.Dispose()
        }
    }
}