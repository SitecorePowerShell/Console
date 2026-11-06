Clear-Host

# #1495: the "Script execution result" rule elements must resolve to real
# condition classes and evaluate script output instead of always returning false.
$stringRuleId = "{A1328FD1-A90C-47C7-B6C5-9E3C1F0BD724}"
$boolRuleId = "{EBBBFA17-9ACC-4B40-93F3-DB13004F5F40}"
$scriptTemplate = "{DD22F1B3-BD87-4DB2-9E7D-F7A496888D43}"

$testRoot = "master:\content\spe-test"
if (Test-Path $testRoot) { Remove-Item $testRoot -Recurse -Force }
$rootItem = New-Item $testRoot -ItemType "Common/Folder"

try {
    $trueScript = New-Item "$testRoot\RuleTrue" -ItemType $scriptTemplate
    $trueScript.Script = '$true'
    $falseScript = New-Item "$testRoot\RuleFalse" -ItemType $scriptTemplate
    $falseScript.Script = '$false'
    $helloScript = New-Item "$testRoot\RuleHello" -ItemType $scriptTemplate
    $helloScript.Script = '"hello"'

    $db = [Sitecore.Configuration.Factory]::GetDatabase("master")
    $parseRules = [Sitecore.Rules.RuleFactory].GetMethods() |
        Where-Object { $_.Name -eq "ParseRules" -and $_.IsGenericMethod -and $_.GetParameters()[1].ParameterType -eq [string] } |
        Select-Object -First 1
    $parseRules = $parseRules.MakeGenericMethod([Sitecore.Rules.RuleContext])
    $equals = (Get-Item "master:\system\Settings\Rules\Definitions\String Operators\Is Equal To").ID.ToString()

    function Test-Rule([string]$ruleId, [string]$scriptId, [string]$extra) {
        $xml = "<ruleset><rule uid=`"{$([guid]::NewGuid())}`"><conditions><condition id=`"$ruleId`" uid=`"{$([guid]::NewGuid())}`" scriptid=`"$scriptId`" $extra /></conditions><actions /></rule></ruleset>"
        $rules = $parseRules.Invoke($null, @($db, $xml))
        $ctx = New-Object Sitecore.Rules.RuleContext
        $ctx.Item = $rootItem
        $rule = $rules.Rules | Select-Object -First 1
        return $rule.Evaluate($ctx)
    }

    "StringMatch=" + (Test-Rule $stringRuleId $helloScript.ID "operatorid=`"$equals`" value=`"hello`"")
    "StringMismatch=" + (Test-Rule $stringRuleId $helloScript.ID "operatorid=`"$equals`" value=`"other`"")
    "BoolTrue=" + (Test-Rule $boolRuleId $trueScript.ID "")
    "BoolFalse=" + (Test-Rule $boolRuleId $falseScript.ID "")
} finally {
    Set-Location master:\content
    Remove-Item $testRoot -Recurse -Force
    "Done!"
}
