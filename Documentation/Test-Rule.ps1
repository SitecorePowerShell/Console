<#
    .SYNOPSIS
        Tests item against a sitecore serialized rules engine rule set.

    .DESCRIPTION
        Tests item or a stream of items against a sitecore serialized rules engine rule set.

    .PARAMETER RuleDatabase 
        Name of the database from which rules are pulled.

    .PARAMETER Rule
        Serialized sitecore rules engine rule. Such rules can be read from rule fields or created by user with the Read-Variable cmdlet.

    .PARAMETER InputObject
        Item to be tested

    .PARAMETER RuleDatabase
        Database in which the rules are located.
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        System.Boolean

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        # Specifies a rule as "items that have layout" and runs the rule againste all items under the ome Item
$rule = '<ruleset>
  <rule uid="{9CF02118-F189-49C4-9F2B-6698D64ACF23}">
    <conditions>
        <condition id="{A45DBBAE-F74F-4EFE-BBD5-24395E0AF945}" uid="ED10990E15EB4E1E8FCFD33F441588A1" />
    </conditions>
  </rule>
</ruleset>'

Get-ChildItem master:\content\Home -Recurse | ? { Test-Rule -InputObject $_ -Rule $rule -RuleDatabase master}

    .EXAMPLE
        # Asks user for the rule and root under which items should be filtered, and lists all items fulfilling the rule under the selected path
$rule = '<ruleset></ruleset>'
$root = Get-Item master:\content\home\ 

$result = Read-Variable -Parameters `
    @{Name="root"; title="Items under"; Tooltip="Items under the selected item will be considered for evaluation"}, `
    @{Name="rule"; Editor="rule"; title="Filter rules"; Tooltip="Only items conforming to this rule will be displayed."} `
    -Description "This dialog shows editor how a rule can be taken from an item and edited using the Read-Variable cmdlet." `
    -Title "Sample rule editing" -Width 600 -Height 600 -ShowHints

if($result -eq "cancel"){
    exit;
}

Get-ChildItem $root.ProviderPath | ? { Test-Rule -InputObject $_ -Rule $rule -RuleDatabase master}

#>
