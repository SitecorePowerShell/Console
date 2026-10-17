Clear-Host

# #1497: the setting-based rule conditions must exist so the Package Generator
# UI can be hidden when Sitecore.Packages.Disabled is true.
$stringSettingCondition = "{26C26920-092C-4763-ADDD-78140463A5A0}"
$hasAncestorCondition = "{D9DD61A1-D6E8-4656-9D82-3FE15CDDC12D}"
$notCaseInsensitivelyEqual = "{6A7294DF-ECAE-4D5F-A8D2-C69CB1161C09}"

function Get-ItemType([string]$id) {
    $item = Get-Item -Path master: -ID $id -ErrorAction SilentlyContinue
    if ($item) { $item["Type"] }
}

"ConditionString=" + ((Get-ItemType $stringSettingCondition) -eq "Spe.Core.Rules.SitecoreConfigSettingValueCondition,Spe")
"ConditionHasAncestor=" + ((Get-ItemType $hasAncestorCondition) -eq "Spe.Core.Rules.HasAncestorInheritingFromTemplateCondition,Spe")
"OperatorName=" + (Get-Item -Path master: -ID $notCaseInsensitivelyEqual).Name

# The Package Generator UI folders carry a ShowRule that hides them when
# Sitecore.Packages.Disabled is true.
$packagingFolder = Get-Item -Path master: -ID "{C83CC1EE-A860-4E64-9715-E9C3BBB885F0}"
$toolboxFolder = Get-Item -Path master: -ID "{A5481454-0C2C-4A54-8BEB-1B51030C40C0}"

$db = [Sitecore.Configuration.Factory]::GetDatabase("master")
$parseRules = [Sitecore.Rules.RuleFactory].GetMethods() |
    Where-Object { $_.Name -eq "ParseRules" -and $_.IsGenericMethod -and $_.GetParameters()[1].ParameterType -eq [string] } |
    Select-Object -First 1
$parseRules = $parseRules.MakeGenericMethod([Sitecore.Rules.RuleContext])

function Test-ShowRule($item, [string]$xml) {
    $rules = $parseRules.Invoke($null, @($db, $xml))
    $ctx = New-Object Sitecore.Rules.RuleContext
    $ctx.Item = $item
    $rule = $rules.Rules | Select-Object -First 1
    if (-not $rule) { return $true }
    return $rule.Evaluate($ctx)
}

# Expected visibility follows this instance's flag (stock 10.5 defaults to true).
$packagesDisabled = [Sitecore.Configuration.Settings]::GetBoolSetting("Sitecore.Packages.Disabled", $false)
$packagingRule = $packagingFolder["ShowRule"]
"PackagesDisabled=$packagesDisabled"
"PackagingMatchesFlag=" + ((Test-ShowRule $packagingFolder $packagingRule) -ne $packagesDisabled)
"ToolboxMatchesFlag=" + ((Test-ShowRule $toolboxFolder $toolboxFolder["ShowRule"]) -ne $packagesDisabled)
"RuleUsesCondition=" + ($packagingRule -like "*$stringSettingCondition*")

$trueSetting = [Sitecore.Configuration.Factory]::GetConfigNodes("settings/setting") |
    Where-Object { $_.Attributes["value"] -and $_.Attributes["value"].Value -ieq "true" } |
    Select-Object -First 1
if ($trueSetting) {
    $trueSettingName = $trueSetting.Attributes["name"].Value
    $hiddenXml = $packagingRule -replace 'Setting="Sitecore\.Packages\.Disabled"', "Setting=`"$trueSettingName`""
    "HiddenWhenTrue=" + (Test-ShowRule $packagingFolder $hiddenXml)
} else {
    "NoTrueSetting"
}
$absentXml = $packagingRule -replace 'Setting="Sitecore\.Packages\.Disabled"', 'Setting="Spe.Test.DoesNotExist"'
"VisibleWhenAbsent=" + (Test-ShowRule $packagingFolder $absentXml)

"Done!"
