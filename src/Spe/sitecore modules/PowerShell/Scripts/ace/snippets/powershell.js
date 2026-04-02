ace.define("ace/snippets/powershell", ["require", "exports", "module"], function(require, exports, module) {
    "use strict";

    exports.snippetText = "\
# General PowerShell\n\
\n\
snippet foreach\n\
	foreach (\\$${1:item} in \\$${2:collection}) {\n\
	    ${0}\n\
	}\n\
\n\
snippet for\n\
	for (\\$${1:i} = ${2:0}; \\$${1:i} -lt ${3:count}; \\$${1:i}++) {\n\
	    ${0}\n\
	}\n\
\n\
snippet while\n\
	while (${1:condition}) {\n\
	    ${0}\n\
	}\n\
\n\
snippet if\n\
	if (${1:condition}) {\n\
	    ${0}\n\
	}\n\
\n\
snippet ifelse\n\
	if (${1:condition}) {\n\
	    ${2}\n\
	} else {\n\
	    ${0}\n\
	}\n\
\n\
snippet switch\n\
	switch (\\$${1:variable}) {\n\
	    \"${2:value}\" { ${3} }\n\
	    default { ${0} }\n\
	}\n\
\n\
snippet trycatch\n\
	try {\n\
	    ${1}\n\
	} catch {\n\
	    Write-Log -Log Error -Message \\$_.Exception.Message\n\
	    ${0}\n\
	}\n\
\n\
snippet function\n\
	function ${1:Verb-Noun} {\n\
	    param(\n\
	        [Parameter(Mandatory)]\n\
	        [${2:string}]\\$${3:Name}\n\
	    )\n\
	    ${0}\n\
	}\n\
\n\
snippet param\n\
	param(\n\
	    [Parameter(Mandatory)]\n\
	    [${1:string}]\\$${2:Name}\n\
	)\n\
\n\
snippet hashtable\n\
	\\$${1:hash} = @{\n\
	    \"${2:Key}\" = \"${3:Value}\"\n\
	}\n\
\n\
snippet pscustomobject\n\
	[PSCustomObject]@{\n\
	    ${1:Property} = ${2:Value}\n\
	}\n\
\n\
# SPE Items\n\
\n\
snippet getitem\n\
	\\$${1:item} = Get-Item -Path \"${2:master}:\" -ID \"${3:{ID\\}}\"\n\
\n\
snippet getchildren\n\
	\\$${1:items} = Get-ChildItem -Path \"${2:master}:${3:/sitecore/content}\" -Recurse\n\
	foreach (\\$item in \\$${1:items}) {\n\
	    ${0}\n\
	}\n\
\n\
snippet newitem\n\
	\\$${1:item} = New-Item -Path \"${2:master:${3:/sitecore/content}}\" -Name \"${4:name}\" -ItemType \"${5:templatePath}\"\n\
\n\
snippet setfield\n\
	\\$${1:item}.Editing.BeginEdit()\n\
	\\$${1:item}[\"${2:FieldName}\"] = \"${3:value}\"\n\
	\\$${1:item}.Editing.EndEdit()\n\
\n\
snippet removeitem\n\
	\\$${1:item} = Get-Item -Path \"${2:master}:${3:/sitecore/content/path}\"\n\
	\\$${1:item} | Remove-Item -Permanently ${0}\n\
\n\
snippet moveitem\n\
	Move-Item -Path \"${1:master:${2:/sitecore/content/source}}\" -Destination \"${3:master:${4:/sitecore/content/target}}\"\n\
\n\
# SPE Search\n\
\n\
snippet finditem\n\
	Find-Item -Index \"${1:sitecore_master_index}\" \\\\\n\
	    -Criteria @{Filter = \"Equals\"; Field = \"${2:_templatename}\"; Value = \"${3:Sample Item}\"} \\\\\n\
	    | Initialize-Item\n\
\n\
snippet findcriteria\n\
	Find-Item -Index \"${1:sitecore_master_index}\" \\\\\n\
	    -Criteria @{Filter = \"Equals\"; Field = \"${2:_templatename}\"; Value = \"${3:Sample Item}\"}, \\\\\n\
	              @{Filter = \"Contains\"; Field = \"${4:_fullpath}\"; Value = \"${5:/sitecore/content}\"} \\\\\n\
	    | Initialize-Item\n\
\n\
# SPE Dialogs\n\
\n\
snippet readvariable\n\
	\\$result = Read-Variable -Parameters \\\\\n\
	    @{ Name = \"${1:selectedItem}\"; Title = \"${2:Choose Item}\"; Source = \"${3:DataSource=/sitecore/content}\"; Editor = \"${4:droptree}\" } \\\\\n\
	    -Description \"${5:Dialog description}\" \\\\\n\
	    -Title \"${6:Dialog title}\" \\\\\n\
	    -OkButtonName \"OK\" -CancelButtonName \"Cancel\"\n\
	if (\\$result -ne \"ok\") { exit }\n\
\n\
snippet showlistview\n\
	\\$${1:items} = Get-ChildItem -Path \"${2:master:${3:/sitecore/content}}\" -Recurse\n\
	\\$${1:items} | Show-ListView -Property @{Label=\"Name\"; Expression={\\$_.DisplayName}}, \\\\\n\
	    @{Label=\"Path\"; Expression={\\$_.ItemPath}}, \\\\\n\
	    @{Label=\"Updated\"; Expression={\\$_.__Updated}} \\\\\n\
	    -Title \"${4:Report title}\"\n\
	Close-Window\n\
\n\
snippet showalert\n\
	Show-Alert -Title \"${1:Message}\"\n\
\n\
snippet showconfirm\n\
	\\$${1:confirmed} = Show-Confirm -Title \"${2:Are you sure?}\"\n\
	if (\\$${1:confirmed} -ne \"yes\") { exit }\n\
\n\
# SPE Publishing\n\
\n\
snippet publishitem\n\
	\\$${1:item} = Get-Item -Path \"${2:master}:${3:/sitecore/content/path}\"\n\
	Publish-Item -Item \\$${1:item} -PublishMode ${4:Smart} -Target \"${5:web}\"\n\
\n\
# SPE Security\n\
\n\
snippet protectitem\n\
	\\$${1:item} = Get-Item -Path \"${2:master}:${3:/sitecore/content/path}\"\n\
	Protect-Item -Item \\$${1:item} -PassThru\n\
\n\
snippet newrole\n\
	New-Role -Identity \"${1:domain\\\\role}\"\n\
\n\
snippet newuser\n\
	\\$${1:user} = New-User -Identity \"${2:domain\\\\username}\" -Enabled -Password (ConvertTo-SecureString -String \"${3:password}\" -AsPlainText -Force)\n\
	Add-RoleMember -Identity \"${4:domain\\\\role}\" -Members \\$${1:user}\n\
\n\
snippet addrole\n\
	Add-RoleMember -Identity \"${1:domain\\\\role}\" -Members \"${2:domain\\\\user}\"\n\
\n\
snippet setrights\n\
	\\$${1:item} = Get-Item -Path \"${2:master}:${3:/sitecore/content/path}\"\n\
	\\$${1:item} | Add-ItemAcl -AccessRight \"${4:item:read}\" -PropagationType \"${5:Any}\" -SecurityPermission AllowAccess -Identity \"${6:domain\\\\role}\"\n\
\n\
snippet getrights\n\
	\\$${1:item} = Get-Item -Path \"${2:master}:${3:/sitecore/content/path}\"\n\
	\\$${1:item} | Get-ItemAcl | Format-Table -Property Identity, AccessRight, SecurityPermission, PropagationType\n\
\n\
snippet lockitem\n\
	\\$${1:item} = Get-Item -Path \"${2:master}:${3:/sitecore/content/path}\"\n\
	\\$${1:item} | Lock-Item\n\
\n\
snippet unlockitem\n\
	\\$${1:item} = Get-Item -Path \"${2:master}:${3:/sitecore/content/path}\"\n\
	\\$${1:item} | Unlock-Item\n\
\n\
# SPE DialogBuilder\n\
\n\
snippet dialogbuilder\n\
	Import-Function -Name DialogBuilder\n\
	\\$dialog = New-DialogBuilder -Title \"${1:Dialog Title}\" -Description \"${2:Description}\" -ShowHints\n\
	\\$dialog | Add-TextField -Name \"${3:fieldName}\" -Title \"${4:Field Label}\" -Mandatory\n\
	${0}\n\
	\\$result = \\$dialog | Invoke-Dialog\n\
	if (\\$result.Result -eq \"ok\") {\n\
	    Write-Host \"Value: \\$${3:fieldName}\"\n\
	}\n\
\n\
snippet dialogfield\n\
	\\$dialog | Add-TextField -Name \"${1:fieldName}\" -Title \"${2:Field Label}\" -Value \"${3}\" -Tooltip \"${4:Help text}\"\n\
\n\
snippet dialogcheckbox\n\
	\\$dialog | Add-Checkbox -Name \"${1:fieldName}\" -Title \"${2:Field Label}\" -Value \\$${3:false}\n\
\n\
snippet dialogdropdown\n\
	\\$dialog | Add-Dropdown -Name \"${1:fieldName}\" -Title \"${2:Field Label}\" -Options @{\"${3:Option A}\" = 1; \"${4:Option B}\" = 2}\n\
\n\
snippet dialogdroptree\n\
	\\$dialog | Add-Droptree -Name \"${1:fieldName}\" -Title \"${2:Choose Item}\" -Root \"${3:/sitecore/content}\"\n\
\n\
snippet dialogdate\n\
	\\$dialog | Add-DateTimePicker -Name \"${1:fieldName}\" -Title \"${2:Select Date}\" -Value ([DateTime]::Now)\n\
\n\
snippet dialogmultilist\n\
	\\$dialog | Add-MultiList -Name \"${1:fieldName}\" -Title \"${2:Select Items}\" -Source \"${3:DataSource=/sitecore/content}\"\n\
\n\
snippet dialogtabs\n\
	\\$dialog | Add-TextField -Name \"${1:field1}\" -Title \"${2:Field 1}\" -Tab \"${3:General}\"\n\
	\\$dialog | Add-TextField -Name \"${4:field2}\" -Title \"${5:Field 2}\" -Tab \"${6:Advanced}\"\n\
\n\
# SPE SearchBuilder\n\
\n\
snippet searchbuilder\n\
	Import-Function -Name SearchBuilder\n\
	\\$search = New-SearchBuilder -Index \"${1:sitecore_master_index}\" -First ${2:25}\n\
	\\$search | Add-TemplateFilter -Name \"${3:Sample Item}\"\n\
	\\$results = \\$search | Invoke-Search\n\
	\\$results.Items | ForEach-Object {\n\
	    ${0}\n\
	}\n\
\n\
snippet searchtemplate\n\
	\\$search | Add-TemplateFilter -Name \"${1:Template Name}\"\n\
\n\
snippet searchfield\n\
	\\$search | Add-FieldContains -Field \"${1:Title}\" -Value \"${2:keyword}\"\n\
\n\
snippet searchequals\n\
	\\$search | Add-FieldEquals -Field \"${1:_language}\" -Value \"${2:en}\"\n\
\n\
snippet searchdate\n\
	\\$search | Add-DateRangeFilter -Field \"${1:__Updated}\" -Last \"${2:7d}\"\n\
\n\
snippet searchgroup\n\
	\\$${1:group} = New-SearchFilterGroup -Operation ${2:Or}\n\
	\\$${1:group} | Add-TemplateFilter -Name \"${3:Template A}\"\n\
	\\$${1:group} | Add-TemplateFilter -Name \"${4:Template B}\"\n\
	\\$search | Add-SearchFilterGroup -Group \\$${1:group}\n\
\n\
snippet searchpaginate\n\
	\\$search = New-SearchBuilder -Index \"${1:sitecore_master_index}\" -First ${2:25}\n\
	\\$search | Add-TemplateFilter -Name \"${3:Sample Item}\"\n\
	do {\n\
	    \\$results = \\$search | Invoke-Search\n\
	    \\$results.Items | ForEach-Object {\n\
	        ${0}\n\
	    }\n\
	} while (\\$results.HasMore)\n\
\n\
# SPE Reporting\n\
\n\
snippet report\n\
	\\$items = Get-ChildItem -Path \"${1:master:${2:/sitecore/content}}\" -Recurse\n\
	if (\\$items.Count -eq 0) {\n\
	    Show-Alert \"No items found matching the criteria.\"\n\
	} else {\n\
	    \\$props = @{\n\
	        Title = \"${3:Report Title}\"\n\
	        InfoTitle = \"${4:Report details}\"\n\
	        InfoDescription = \"${5:Description of what this report shows.}\"\n\
	        PageSize = 25\n\
	        Property = @(\n\
	            @{Label=\"Name\"; Expression={\\$_.DisplayName}},\n\
	            @{Label=\"Path\"; Expression={\\$_.ItemPath}},\n\
	            @{Label=\"Updated\"; Expression={\\$_.__Updated}},\n\
	            @{Label=\"Updated by\"; Expression={\\$_.\"__Updated by\"}}\n\
	        )\n\
	    }\n\
	    \\$items | Show-ListView @props\n\
	}\n\
	Close-Window\n\
\n\
# SPE Import/Export\n\
\n\
snippet csvimport\n\
	\\$uploadPath = \"${1:temp}\"\n\
	\\$filePath = Receive-File -Overwrite -Title \"${2:Import Data}\" -Path \\$uploadPath\n\
	\\$data = Import-Csv \\$filePath\n\
	foreach (\\$row in \\$data) {\n\
	    ${0}\n\
	}\n\
\n\
snippet csvexport\n\
	\\$items = Get-ChildItem -Path \"${1:master:${2:/sitecore/content}}\" -Recurse\n\
	\\$items | Select-Object -Property Name, @{Label=\"Path\"; Expression={\\$_.ItemPath}} |\n\
	    Export-Csv -Path \"${3:\\$SitecoreDataFolder\\\\export.csv}\" -NoTypeInformation\n\
\n\
# SPE Tasks & Logging\n\
\n\
snippet jobprogress\n\
	\\$items = Get-ChildItem -Path \"${1:master:${2:/sitecore/content}}\" -Recurse\n\
	\\$total = \\$items.Count\n\
	for (\\$i = 0; \\$i -lt \\$total; \\$i++) {\n\
	    \\$item = \\$items[\\$i]\n\
	    [Sitecore.Context]::Job.Status.Processed = \\$i + 1\n\
	    [Sitecore.Context]::Job.Status.Messages.Add(\"Processing \\$(\\$item.Name)\") > \\$null\n\
	    ${0}\n\
	}\n\
\n\
snippet writelog\n\
	Write-Log \"${1:message}\"\n\
\n\
snippet importfunction\n\
	Import-Function -Name ${1:FunctionName}\n\
\n\
# SPE Advanced Functions\n\
\n\
snippet filter\n\
	filter ${1:Skip-Item} {\n\
	    param(\n\
	        [Parameter(Mandatory, ValueFromPipeline)]\n\
	        [Sitecore.Data.Items.Item]\\$Item\n\
	    )\n\
	    if (${2:condition}) {\n\
	        \\$Item\n\
	    }\n\
	}\n\
\n\
snippet beginprocessend\n\
	function ${1:Verb-Noun} {\n\
	    [CmdletBinding()]\n\
	    param(\n\
	        [Parameter(Mandatory, ValueFromPipeline)]\n\
	        [Sitecore.Data.Items.Item]\\$Item\n\
	    )\n\
	    begin {\n\
	        ${2}\n\
	    }\n\
	    process {\n\
	        ${0}\n\
	    }\n\
	    end {\n\
	    }\n\
	}\n\
\n\
snippet bulkupdate\n\
	\\$items = Get-ChildItem -Path \"${1:master:${2:/sitecore/content}}\" -Recurse\n\
	foreach (\\$item in \\$items) {\n\
	    \\$item.Editing.BeginEdit()\n\
	    \\$item[\"${3:FieldName}\"] = \"${4:value}\"\n\
	    \\$item.Editing.EndEdit() > \\$null\n\
	}\n\
\n\
snippet sqlquery\n\
	Import-Function -Name Invoke-SqlCommand\n\
	\\$connection = [Sitecore.Configuration.Settings]::GetConnectionString(\"${1:master}\")\n\
	\\$query = @\"\n\
	SELECT ${2:*} FROM [dbo].[${3:Items}]\n\
	WHERE ${4:condition}\n\
	\"@\n\
	\\$results = Invoke-SqlCommand -Connection \\$connection -Query \\$query\n\
";
    exports.scope = "powershell";

});
