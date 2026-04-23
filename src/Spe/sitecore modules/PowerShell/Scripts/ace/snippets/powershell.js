ace.define(
  "ace/snippets/powershell",
  ["require", "exports", "module"],
  function (require, exports, module) {
    "use strict";

    exports.snippetText =
      '\
# General PowerShell\n\
\n\
snippet spe-foreach\n\
description Iterate over a collection with foreach loop.\n\
	foreach (\\$${1:item} in \\$${2:collection}) {\n\
	    ${0}\n\
	}\n\
\n\
snippet spe-for\n\
description Counted for loop with index variable.\n\
	for (\\$${1:i} = ${2:0}; \\$${1:i} -lt ${3:count}; \\$${1:i}++) {\n\
	    ${0}\n\
	}\n\
\n\
snippet spe-while\n\
description Loop while a condition is true.\n\
	while (${1:condition}) {\n\
	    ${0}\n\
	}\n\
\n\
snippet spe-if\n\
description Conditional if block.\n\
	if (${1:condition}) {\n\
	    ${0}\n\
	}\n\
\n\
snippet spe-ifelse\n\
description Conditional if/else block.\n\
	if (${1:condition}) {\n\
	    ${2}\n\
	} else {\n\
	    ${0}\n\
	}\n\
\n\
snippet spe-switch\n\
description Switch statement with default case.\n\
	switch (\\$${1:variable}) {\n\
	    "${2:value}" { ${3} }\n\
	    default { ${0} }\n\
	}\n\
\n\
snippet spe-trycatch\n\
description Try/catch block with error logging.\n\
	try {\n\
	    ${1}\n\
	} catch {\n\
	    Write-Log -Log Error -Message \\$_.Exception.Message\n\
	    ${0}\n\
	}\n\
\n\
snippet spe-function\n\
description Function with param block.\n\
	function ${1:Verb-Noun} {\n\
	    param(\n\
	        [Parameter(Mandatory)]\n\
	        [${2:string}]\\$${3:Name}\n\
	    )\n\
	    ${0}\n\
	}\n\
\n\
snippet spe-param\n\
description Standalone param block with mandatory parameter.\n\
	param(\n\
	    [Parameter(Mandatory)]\n\
	    [${1:string}]\\$${2:Name}\n\
	)\n\
\n\
snippet spe-hashtable\n\
description Create a hashtable variable.\n\
	\\$${1:hash} = @{\n\
	    "${2:Key}" = "${3:Value}"\n\
	}\n\
\n\
snippet spe-pscustomobject\n\
description Create a PSCustomObject with named properties.\n\
	[PSCustomObject]@{\n\
	    ${1:Property} = ${2:Value}\n\
	}\n\
\n\
# SPE Items\n\
\n\
snippet spe-getitem\n\
description Get a Sitecore item by ID.\n\
	\\$${1:item} = Get-Item -Path "${2:master}:" -ID "${3:{ID\\}}"\n\
\n\
snippet spe-getchildren\n\
description Get child items recursively and iterate.\n\
	\\$${1:items} = Get-ChildItem -Path "${2:master}:${3:/sitecore/content}" -Recurse\n\
	foreach (\\$item in \\$${1:items}) {\n\
	    ${0}\n\
	}\n\
\n\
snippet spe-newitem\n\
description Create a new Sitecore item from a template.\n\
	\\$${1:item} = New-Item -Path "${2:master:${3:/sitecore/content}}" -Name "${4:name}" -ItemType "${5:templatePath}"\n\
\n\
snippet spe-setfield\n\
description Edit a field value on an item.\n\
	\\$${1:item}.Editing.BeginEdit()\n\
	\\$${1:item}["${2:FieldName}"] = "${3:value}"\n\
	\\$${1:item}.Editing.EndEdit()\n\
\n\
snippet spe-removeitem\n\
description Remove a Sitecore item permanently.\n\
	\\$${1:item} = Get-Item -Path "${2:master}:${3:/sitecore/content/path}"\n\
	\\$${1:item} | Remove-Item -Permanently ${0}\n\
\n\
snippet spe-moveitem\n\
description Move an item to a new location.\n\
	Move-Item -Path "${1:master:${2:/sitecore/content/source}}" -Destination "${3:master:${4:/sitecore/content/target}}"\n\
\n\
# SPE Search\n\
\n\
snippet spe-finditem\n\
description Search index query with single filter.\n\
	Find-Item -Index "${1:sitecore_master_index}" \\\\\n\
	    -Criteria @{Filter = "Equals"; Field = "${2:_templatename}"; Value = "${3:Sample Item}"} \\\\\n\
	    | Initialize-Item\n\
\n\
snippet spe-findcriteria\n\
description Search index query with multiple criteria.\n\
	Find-Item -Index "${1:sitecore_master_index}" \\\\\n\
	    -Criteria @{Filter = "Equals"; Field = "${2:_templatename}"; Value = "${3:Sample Item}"}, \\\\\n\
	              @{Filter = "Contains"; Field = "${4:_fullpath}"; Value = "${5:/sitecore/content}"} \\\\\n\
	    | Initialize-Item\n\
\n\
# SPE Dialogs\n\
\n\
snippet spe-readvariable\n\
description Raw Read-Variable call with inline parameter hashtable.\n\
	\\$result = Read-Variable -Parameters \\\\\n\
	    @{ Name = "${1:selectedItem}"; Title = "${2:Choose Item}"; Source = "${3:DataSource=/sitecore/content}"; Editor = "${4:droptree}" } \\\\\n\
	    -Description "${5:Dialog description}" \\\\\n\
	    -Title "${6:Dialog title}" \\\\\n\
	    -OkButtonName "OK" -CancelButtonName "Cancel"\n\
	if (\\$result -ne "ok") { exit }\n\
\n\
snippet spe-showlistview\n\
description Display items in an interactive list view report.\n\
	\\$${1:items} = Get-ChildItem -Path "${2:master:${3:/sitecore/content}}" -Recurse\n\
	\\$${1:items} | Show-ListView -Property @{Label="Name"; Expression={\\$_.DisplayName}}, \\\\\n\
	    @{Label="Path"; Expression={\\$_.ItemPath}}, \\\\\n\
	    @{Label="Updated"; Expression={\\$_.__Updated}} \\\\\n\
	    -Title "${4:Report title}"\n\
	Close-Window\n\
\n\
snippet spe-dialogtreelist\n\
description Full DialogBuilder script with a treelist picker.\n\
	Import-Function -Name DialogBuilder\n\
	\\$dialog = New-DialogBuilder -Title "${1:Treelist Dialog}" \\\\\n\
	    -Description "${2:Pick one or more items from the tree.}"\n\
	\\$dialog | Add-TreeList -Name "${3:items}" -Title "${4:Select Items}" \\\\\n\
	    -Source "${5:DataSource=/sitecore/templates&DatabaseName=master&IncludeTemplatesForDisplay=Node,Folder,Template,Template Folder&IncludeTemplatesForSelection=Template}"\n\
	\\$result = \\$dialog | Invoke-Dialog\n\
	if (\\$result.Result -ne "ok") { exit }\n\
\n\
snippet spe-dialogquickdemo\n\
description Compact DialogBuilder demo with common controls and tabs.\n\
	Import-Function -Name DialogBuilder\n\
	\\$options = [ordered]@{"Option A" = 1; "Option B" = 2; "Option C" = 4}\n\
	\\$selection = [ordered]@{"None" = 1; "Checklist" = 2; "Radio" = 3}\n\
	\n\
	\\$dialog = New-DialogBuilder -Title "Kitchen Sink Demo" \\\\\n\
	    -Description "Demonstrates all available DialogBuilder controls." \\\\\n\
	    -Width 650 -Height 700 -ShowHints\n\
	\n\
	# Simple tab\n\
	\\$dialog | Add-TextField -Name "someText" -Title "Text" -Tooltip "Single line text" -Tab "Simple" -Placeholder "Enter text"\n\
	\\$dialog | Add-MultiLineTextField -Name "multiText" -Title "Multiline Text" -Tab "Simple" -Placeholder "Enter text"\n\
	\\$dialog | Add-DialogField -Name "number" -Title "Number" -Value 0 -Editor "number" -Tab "Simple"\n\
	\\$dialog | Add-DialogField -Name "password" -Title "Password" -Editor "password" -Tab "Simple"\n\
	\\$dialog | Add-Checkbox -Name "toggleOn" -Title "Checkbox" -Value \\$true -Tab "Simple"\n\
	\n\
	# Options tab\n\
	\\$dialog | Add-Dropdown -Name "anOption" -Title "Dropdown" -Options \\$selection -Tab "Options"\n\
	\\$dialog | Add-Checklist -Name "checklistItems" -Title "Checklist" -Options \\$options -Tab "Options"\n\
	\\$dialog | Add-RadioButtons -Name "radioListItems" -Title "Radio" -Options \\$selection -Tab "Options"\n\
	\n\
	# Time tab\n\
	\\$dialog | Add-DateTimePicker -Name "from" -Title "Date Time" -Value ([DateTime]::Now.AddDays(-5)) -Tab "Time"\n\
	\\$dialog | Add-DialogField -Name "fromDate" -Title "Date" -Value ([DateTime]::Now) -Editor "date" -Tab "Time"\n\
	\n\
	# Items tab\n\
	\\$dialog | Add-Droptree -Name "item" -Title "Item" -Root "/sitecore/content/" -Tab "Items"\n\
	\\$dialog | Add-TreeList -Name "items" -Title "Treelist" \\\\\n\
	    -Source "DataSource=/sitecore/templates&DatabaseName=master&IncludeTemplatesForDisplay=Node,Folder,Template,Template Folder&IncludeTemplatesForSelection=Template" \\\\\n\
	    -Tab "Items"\n\
	\\$dialog | Add-MultiList -Name "items2" -Title "Multilist" \\\\\n\
	    -Source "DataSource=/sitecore/templates&DatabaseName=master&IncludeTemplatesForDisplay=Node,Folder,Template,Template Folder&IncludeTemplatesForSelection=Template" \\\\\n\
	    -Tab "Items"\n\
	\n\
	# Rights tab\n\
	\\$dialog | Add-UserPicker -Name "user" -Title "User" -Tab "Rights"\n\
	\\$dialog | Add-RolePicker -Name "role" -Title "Role" -Tab "Rights"\n\
	\n\
	# Rules tab\n\
	\\$dialog | Add-RuleField -Name "rule" -Title "Rule" -Tab "Rules"\n\
	\n\
	\\$result = \\$dialog | Invoke-Dialog\n\
	if (\\$result.Result -ne "ok") { exit }\n\
\n\
snippet spe-showalert\n\
description Show a simple alert message box.\n\
	Show-Alert -Title "${1:Message}"\n\
\n\
snippet spe-showconfirm\n\
description Show a yes/no confirmation dialog.\n\
	\\$${1:confirmed} = Show-Confirm -Title "${2:Are you sure?}"\n\
	if (\\$${1:confirmed} -ne "yes") { exit }\n\
\n\
# SPE Publishing\n\
\n\
snippet spe-publishitem\n\
description Publish a Sitecore item to a target database.\n\
	\\$${1:item} = Get-Item -Path "${2:master}:${3:/sitecore/content/path}"\n\
	Publish-Item -Item \\$${1:item} -PublishMode ${4:Smart} -Target "${5:web}"\n\
\n\
# SPE Security\n\
\n\
snippet spe-protectitem\n\
description Protect an item from editing.\n\
	\\$${1:item} = Get-Item -Path "${2:master}:${3:/sitecore/content/path}"\n\
	Protect-Item -Item \\$${1:item} -PassThru\n\
\n\
snippet spe-newrole\n\
description Create a new security role.\n\
	New-Role -Identity "${1:domain\\\\role}"\n\
\n\
snippet spe-newuser\n\
description Create a new user and assign to a role.\n\
	\\$${1:user} = New-User -Identity "${2:domain\\\\username}" -Enabled -Password (ConvertTo-SecureString -String "${3:password}" -AsPlainText -Force)\n\
	Add-RoleMember -Identity "${4:domain\\\\role}" -Members \\$${1:user}\n\
\n\
snippet spe-addrole\n\
description Add a user to a security role.\n\
	Add-RoleMember -Identity "${1:domain\\\\role}" -Members "${2:domain\\\\user}"\n\
\n\
snippet spe-setrights\n\
description Set access rights on a Sitecore item.\n\
	\\$${1:item} = Get-Item -Path "${2:master}:${3:/sitecore/content/path}"\n\
	\\$${1:item} | Add-ItemAcl -AccessRight "${4:item:read}" -PropagationType "${5:Any}" -SecurityPermission AllowAccess -Identity "${6:domain\\\\role}"\n\
\n\
snippet spe-getrights\n\
description Display access rights on a Sitecore item.\n\
	\\$${1:item} = Get-Item -Path "${2:master}:${3:/sitecore/content/path}"\n\
	\\$${1:item} | Get-ItemAcl | Format-Table -Property Identity, AccessRight, SecurityPermission, PropagationType\n\
\n\
snippet spe-lockitem\n\
description Lock a Sitecore item for editing.\n\
	\\$${1:item} = Get-Item -Path "${2:master}:${3:/sitecore/content/path}"\n\
	\\$${1:item} | Lock-Item\n\
\n\
snippet spe-unlockitem\n\
description Unlock a Sitecore item.\n\
	\\$${1:item} = Get-Item -Path "${2:master}:${3:/sitecore/content/path}"\n\
	\\$${1:item} | Unlock-Item\n\
\n\
# SPE DialogBuilder\n\
\n\
snippet spe-dialogbuilder\n\
description DialogBuilder starter template with one field.\n\
	Import-Function -Name DialogBuilder\n\
	\\$dialog = New-DialogBuilder -Title "${1:Dialog Title}" -Description "${2:Description}" -ShowHints\n\
	\\$dialog | Add-TextField -Name "${3:fieldName}" -Title "${4:Field Label}" -Mandatory\n\
	${0}\n\
	\\$result = \\$dialog | Invoke-Dialog\n\
	if (\\$result.Result -eq "ok") {\n\
	    Write-Host "Value: \\$${3:fieldName}"\n\
	}\n\
\n\
snippet spe-dialogkitchensink\n\
description Full DialogBuilder demo with every control type, tabs, and conditional visibility.\n\
	Import-Function -Name DialogBuilder\n\
	\n\
	# Pre-initialize item values\n\
	\\$item = Get-Item -Path "master:\\content\\Home"\n\
	\\$items = Get-ChildItem -Path "master:\\templates\\Modules\\PowerShell Console\\PowerShell Script*"\n\
	\\$items2 = \\$items\n\
	\\$droplistItem = \\$items | Select-Object -First 1\n\
	\\$droplinkItem = \\$items | Select-Object -First 1\n\
	\\$droptreeItem = \\$items | Select-Object -First 1\n\
	\\$selectedGroupedDroplink = Get-Item -Path "master:" -ID "{76036F5E-CBCE-46D1-AF0A-4143F9B557AA}"\n\
	\\$selectedGroupedDroplist = Get-Item -Path "master:" -ID "{76036F5E-CBCE-46D1-AF0A-4143F9B557AA}"\n\
	\\$parent = Get-Item -Path . | Select-Object -Expand Parent\n\
	\\$checklistItems = @(4,16)\n\
	\\$radioListItems = 3\n\
	\\$rule = Get-Item -Path "master:" -ID "{D00BD134-EB15-41A7-BEF1-E6455C6BC9AC}" | Select-Object -Expand ShowRule\n\
	\n\
	\\$options = [ordered]@{Monday = 1; Tuesday = 2; "Wednesday (Selected)" = 4; Thursday = 8; "Friday (Selected)" = 16; Saturday = 32; Sunday = 64}\n\
	\\$selection = [ordered]@{"None" = 1; "Checklist" = 2; "Radio Buttons" = 3}\n\
	\\$selectionTooltips = [ordered]@{1 = "No options hidden from the user."}\n\
	\n\
	\\$dialog = New-DialogBuilder -Title "Kitchen Sink Demo" -Description "The dialog demonstrates the use of all available controls grouped into tabs." -Width 650 -Height 700 -OkButtonName "Continue" -CancelButtonName "Cancel" -ShowHints -Icon "Officewhite/32x32/knife_fork_spoon.png"\n\
	\n\
	# --- Tab: Simple ---\n\
	\\$dialog | Add-Checkbox -Name "toggleVisible" -Title "Show Controls - checkbox" -Value \\$true -Tooltip "Checking or unchecking will change hidden state" -Tab "Simple" -Columns 4 -GroupId 1\n\
	\\$dialog | Add-Marquee -Name "marquee" -Value "Interesting details about the dialogs." -Tab "Simple" -ParentGroupId 1 -HideOnValue "false"\n\
	\\$dialog | Add-InfoText -Name "Info" -Title "Information - info" -Value "Interesting details about the dialogs." -Tab "Simple" -ParentGroupId 1 -HideOnValue "false"\n\
	\\$dialog | Add-TextField -Name "someText" -Title "Text" -Tooltip "Just a single line of Text" -Tab "Simple" -Placeholder "You see this when text box is empty" -ParentGroupId 1 -HideOnValue "false"\n\
	\\$dialog | Add-MultiLineTextField -Name "multiText" -Title "Longer Text" -Lines 3 -Tooltip "You can put multi line text here" -Tab "Simple" -Placeholder "You see this when text box is empty" -ParentGroupId 1 -HideOnValue "false"\n\
	\\$dialog | Add-TextField -Name "number" -Title "Integer" -IsNumber -Value 110 -Tooltip "I need this number too" -Tab "Simple" -Columns 6 -ParentGroupId 1 -HideOnValue "false"\n\
	\\$dialog | Add-DialogField -Name "fraction" -Title "Float" -Value 1.1 -Editor "number" -Tooltip "I\'m just a bit over 1" -Tab "Simple" -Columns 6 -ParentGroupId 1 -HideOnValue "false"\n\
	\\$dialog | Add-TextField -Name "username" -Title "Username" -Tooltip "Enter username here" -Tab "Simple" -Placeholder "You see this when text box is empty" -Columns 6 -ParentGroupId 1 -HideOnValue "false"\n\
	\\$dialog | Add-TextField -Name "password" -Title "Password" -IsPassword -Value "password" -Tooltip "Enter password here" -Tab "Simple" -Placeholder "You see this when text box is empty" -Columns 6 -ParentGroupId 1 -HideOnValue "false"\n\
	\\$dialog | Add-TextField -Name "seoTitle" -Title "SEO Title" -MaxLength 60 -Tooltip "Keep under 60 chars for search engines" -Tab "Simple" -ParentGroupId 1 -HideOnValue "false"\n\
	\\$dialog | Add-MultiLineTextField -Name "metaDesc" -Title "Meta Description" -Lines 2 -MaxLength 160 -Tooltip "Keep under 160 chars" -Tab "Simple" -ParentGroupId 1 -HideOnValue "false"\n\
	\n\
	# --- Tab: Options ---\n\
	\\$dialog | Add-Dropdown -Name "anOption" -Title "An Option - combo" -Value "1" -Options \\$selection -OptionTooltips \\$selectionTooltips -Tooltip "Choose a control to hide" -Tab "Options" -GroupId 2\n\
	\\$dialog | Add-Checklist -Name "checklistItems" -Title "Multiple options - checklist" -Value \\$checklistItems -Options \\$options -Tooltip "Checklist with various options" -Tab "Options" -ParentGroupId 2 -HideOnValue "2"\n\
	\\$dialog | Add-RadioButtons -Name "radioListItems" -Title "Radio selection - radio" -Value \\$radioListItems -Options \\$selection -Tab "Options" -ParentGroupId 2 -HideOnValue "3"\n\
	\n\
	# --- Tab: Time ---\n\
	\\$dialog | Add-DateTimePicker -Name "from" -Title "Start Date" -Value ([DateTime]::Now.AddDays(-5)) -Tooltip "Date since when you want the report to run" -Tab "Time"\n\
	\\$dialog | Add-DateTimePicker -Name "fromDate" -Title "Start Date" -Value ([DateTime]::Now.AddDays(-5)) -Tooltip "Date since when you want the report to run" -DateOnly -Tab "Time"\n\
	\n\
	# --- Tab: Items ---\n\
	\\$dialog | Add-ItemPicker -Name "item" -Title "Start Item - item" -Root "/sitecore/content/" -Tab "Items"\n\
	\\$dialog | Add-DialogField -Name "parent" -Title "Parent - variable" -Value \\$parent -Tab "Items"\n\
	\\$dialog | Add-TreeList -Name "items" -Title "Bunch of Templates - treelist" -Value \\$items -Source "DataSource=/sitecore/templates&DatabaseName=master&IncludeTemplatesForDisplay=Node,Folder,Template,Template Folder&IncludeTemplatesForSelection=Template" -Tab "Items"\n\
	\n\
	# --- Tab: More Items ---\n\
	\\$dialog | Add-MultiList -Name "items2" -Title "Bunch of Templates - multilist" -Value \\$items2 -Source "DataSource=/sitecore/templates/modules/powershell console&DatabaseName=master&IncludeTemplatesForDisplay=Node,Folder,Template,Template Folder&IncludeTemplatesForSelection=Template" -Height "300px" -Tab "More Items"\n\
	\\$dialog | Add-DialogField -Name "items4" -Title "Bunch of Templates with Search - multilist search" -Editor "multilist search" -Source "DataSource=/sitecore/templates/modules/powershell console&DatabaseName=master&IncludeTemplatesForDisplay=Node,Folder,Template,Template Folder&IncludeTemplatesForSelection=Template" -Height "300px" -Tab "More Items"\n\
	\\$dialog | Add-Droplist -Name "droplistItem" -Title "Pick One Template - droplist" -Value \\$droplistItem -Source "DataSource=/sitecore/templates/modules/powershell console&DatabaseName=master&IncludeTemplatesForDisplay=Node,Folder,Template,Template Folder&IncludeTemplatesForSelection=Template" -Tab "More Items"\n\
	\\$dialog | Add-Droplink -Name "droplinkItem" -Title "Pick One Template - droplink" -Value \\$droplinkItem -Source "DataSource=/sitecore/templates/modules/powershell console&DatabaseName=master&IncludeTemplatesForDisplay=Node,Folder,Template,Template Folder&IncludeTemplatesForSelection=Template" -Tab "More Items"\n\
	\\$dialog | Add-Droptree -Name "droptreeItem" -Title "Pick One Template - droptree" -Value \\$droptreeItem -Source "DataSource=/sitecore/templates/modules/powershell console&DatabaseName=master&IncludeTemplatesForDisplay=Node,Folder,Template,Template Folder&IncludeTemplatesForSelection=Template" -Tab "More Items"\n\
	\\$dialog | Add-GroupedDroplink -Name "selectedGroupedDroplink" -Title "Pick One Template - groupeddroplink" -Value \\$selectedGroupedDroplink -Source "DataSource=/sitecore/templates&DatabaseName=master&IncludeTemplatesForDisplay=Node,Folder,Template,Template Folder&IncludeTemplatesForSelection=Template" -Tab "More Items"\n\
	\\$dialog | Add-GroupedDroplist -Name "selectedGroupedDroplist" -Title "Pick One Template - groupeddroplist" -Value \\$selectedGroupedDroplist -Source "DataSource=/sitecore/templates&DatabaseName=master&IncludeTemplatesForDisplay=Node,Folder,Template,Template Folder&IncludeTemplatesForSelection=Template" -Tab "More Items"\n\
	\\$dialog | Add-Droplist -Name "optionalLayout" -Title "Optional Layout - droplist + AllowNone" -Value \\$null -Source "DataSource=/sitecore/layout/layouts" -AllowNone -Placeholder "Select a layout..." -Tab "More Items"\n\
	\\$dialog | Add-Droplink -Name "optionalDroplink" -Title "Optional Template - droplink + AllowNone" -Value \\$null -Source "DataSource=/sitecore/templates/modules/powershell console&DatabaseName=master&IncludeTemplatesForDisplay=Node,Folder,Template,Template Folder&IncludeTemplatesForSelection=Template" -AllowNone -Placeholder "Select a template..." -Tab "More Items"\n\
	\\$dialog | Add-Droptree -Name "optionalDroptree" -Title "Optional Template - droptree + AllowNone" -Value \\$null -Source "DataSource=/sitecore/templates/modules/powershell console&DatabaseName=master&IncludeTemplatesForDisplay=Node,Folder,Template,Template Folder&IncludeTemplatesForSelection=Template" -AllowNone -Tab "More Items"\n\
	\\$dialog | Add-GroupedDroplink -Name "optionalGroupedDroplink" -Title "Optional Template - groupeddroplink + AllowNone" -Value \\$null -Source "DataSource=/sitecore/templates&DatabaseName=master&IncludeTemplatesForDisplay=Node,Folder,Template,Template Folder&IncludeTemplatesForSelection=Template" -AllowNone -Placeholder "Select a template..." -Tab "More Items"\n\
	\\$dialog | Add-GroupedDroplist -Name "optionalGroupedDroplist" -Title "Optional Template - groupeddroplist + AllowNone" -Value \\$null -Source "DataSource=/sitecore/templates&DatabaseName=master&IncludeTemplatesForDisplay=Node,Folder,Template,Template Folder&IncludeTemplatesForSelection=Template" -AllowNone -Placeholder "Select a template..." -Tab "More Items"\n\
	\\$dialog | Add-ItemPicker -Name "optionalStart" -Title "Optional Start Item - item + AllowNone" -Value \\$null -Root "/sitecore/content" -AllowNone -Tab "More Items"\n\
	\n\
	# --- Tab: Rights ---\n\
	\\$dialog | Add-UserPicker -Name "user" -Title "Select User" -Value \\$me -Tooltip "Tooltip for user" -Multiple -Tab "Rights"\n\
	\\$dialog | Add-RolePicker -Name "role" -Title "Select Role" -Tooltip "Tooltip for role" -Multiple -Domain "sitecore" -Tab "Rights"\n\
	\\$dialog | Add-UserRolePicker -Name "userOrRole" -Title "Select User or Role" -Tooltip "Tooltip for role" -Multiple -Domain "sitecore" -Tab "Rights"\n\
	\n\
	# --- Tab: Rules ---\n\
	\\$dialog | Add-RuleField -Name "rule" -Title "A rule" -Value \\$rule -Tooltip "A sample rule editor" -Tab "Rules"\n\
	\\$dialog | Add-RuleActionField -Name "rulewithaction" -Title "A rule" -Tooltip "A sample rule editor" -Tab "Rules"\n\
	\n\
	# --- Invoke ---\n\
	\\$result = \\$dialog | Invoke-Dialog\n\
	if (\\$result.Result -ne "ok") { exit }\n\
	\n\
	# --- Second dialog (no tabs) ---\n\
	\\$dialog2 = New-DialogBuilder -Title "Kitchen Sink Demo" -Description "The dialog demonstrates the use of some available controls without tabs." -Width 500 -Height 480 -OkButtonName "Finish"\n\
	\\$dialog2 | Add-TextField -Name "someText" -Title "Single Line Text" -Value "Some Text" -Tooltip "Tooltip for singleline" -Placeholder "You see this when text box is empty"\n\
	\\$dialog2 | Add-MultiLineTextField -Name "multiText" -Title "Multi Line Text" -Value "Multiline Text" -Lines 3 -Tooltip "Tooltip for multiline" -Placeholder "You see this when text box is empty"\n\
	\\$dialog2 | Add-DateTimePicker -Name "from" -Title "Start Date" -Value ([DateTime]::Now.AddDays(-5)) -Tooltip "Date since when you want the report to run"\n\
	\\$dialog2 | Add-UserPicker -Name "user" -Title "Select User" -Value \\$me -Tooltip "Tooltip for user" -Multiple\n\
	\\$dialog2 | Add-ItemPicker -Name "item" -Title "Start Item" -Root "/sitecore/content/"\n\
	\\$result = \\$dialog2 | Invoke-Dialog\n\
	\n\
	# --- Output variable types ---\n\
	"Variables from dialog:"\n\
	Write-Host \'Editor date time: returns a DateTime value\' -ForegroundColor Green\n\
	\\$from | Out-Default\n\
	Write-Host \'Editor date: returns a DateTime value\' -ForegroundColor Green\n\
	\\$fromDate | Out-Default\n\
	Write-Host \'Editor item: returns an Item\' -ForegroundColor Green\n\
	\\$item | Out-Default\n\
	Write-Host \'Variable: returns an Item\' -ForegroundColor Green\n\
	\\$parent | Out-Default\n\
	Write-Host \'Editor checkbox: returns a bool value\' -ForegroundColor Green\n\
	\\$toggleVisible | Out-Default\n\
	Write-Host \'Editor text: returns a string value\' -ForegroundColor Green\n\
	\\$someText | Out-Default\n\
	Write-Host \'Editor multitext: returns a string value\' -ForegroundColor Green\n\
	\\$multiText | Out-Default\n\
	Write-Host \'Editor password: returns a string value\' -ForegroundColor Green\n\
	\\$password | Out-Default\n\
	Write-Host \'Editor text with MaxLength: returns a string value\' -ForegroundColor Green\n\
	\\$seoTitle | Out-Default\n\
	Write-Host \'Editor multitext with MaxLength: returns a string value\' -ForegroundColor Green\n\
	\\$metaDesc | Out-Default\n\
	Write-Host \'Editor combo: returns a string value\' -ForegroundColor Green\n\
	\\$anOption | Out-Default\n\
	Write-Host \'Editor checklist: returns an array of string values\' -ForegroundColor Green\n\
	\\$checklistItems | Out-Default\n\
	Write-Host \'Editor radio: returns a string value\' -ForegroundColor Green\n\
	\\$radioListItems | Out-Default\n\
	Write-Host \'Editor number: returns a string field value\' -ForegroundColor Green\n\
	\\$number | Out-Default\n\
	Write-Host \'Editor float: returns a double value\' -ForegroundColor Green\n\
	\\$fraction | Out-Default\n\
	Write-Host \'Editor user: returns a string value\' -ForegroundColor Green\n\
	\\$user | Out-Default\n\
	Write-Host \'Editor role: returns a string value\' -ForegroundColor Green\n\
	\\$role | Out-Default\n\
	Write-Host \'Editor user role: returns an array of string values\' -ForegroundColor Green\n\
	\\$userOrRole | Out-Default\n\
	Write-Host \'Editor rule: returns a string value\' -ForegroundColor Green\n\
	\\$rule | Out-Default\n\
	Write-Host \'Editor rule action: returns a string field value\' -ForegroundColor Green\n\
	\\$rulewithaction | Out-Default\n\
	Write-Host \'Editor treelist: returns an array of Item\' -ForegroundColor Green\n\
	\\$items | Out-Default\n\
	Write-Host \'Editor multilist: returns an array of Item\' -ForegroundColor Green\n\
	\\$items2 | Out-Default\n\
	Write-Host \'Editor droplist: returns an Item\' -ForegroundColor Green\n\
	\\$items3 | Out-Default\n\
	Write-Host \'Editor multilist search: returns an array of Item\' -ForegroundColor Green\n\
	\\$items4 | Out-Default\n\
	Write-Host \'Editor GroupedDroplist: returns the string value\' -ForegroundColor Green\n\
	\\$selectedGroupedDroplist | Out-Default\n\
	Write-Host \'Editor GroupedDroplink: returns an Item\' -ForegroundColor Green\n\
	\\$selectedGroupedDroplink | Out-Default\n\
\n\
snippet spe-dialogfield\n\
description Single-line text input. Returns string.\n\
	\\$dialog | Add-TextField -Name "${1:fieldName}" -Title "${2:Field Label}" -Value "${3}" -Tooltip "${4:Help text}"\n\
\n\
snippet spe-dialogpassword\n\
description Masked password input. Returns string.\n\
	\\$dialog | Add-TextField -Name "${1:fieldName}" -Title "${2:Password}" -IsPassword\n\
\n\
snippet spe-dialogemail\n\
description Email input with browser validation. Returns string.\n\
	\\$dialog | Add-TextField -Name "${1:fieldName}" -Title "${2:Email}" -IsEmail -Placeholder "${3:user@example.com}"\n\
\n\
snippet spe-dialognumber\n\
description Numeric input. Returns string (use [int] cast).\n\
	\\$dialog | Add-TextField -Name "${1:fieldName}" -Title "${2:Count}" -IsNumber -Value ${3:0}\n\
\n\
snippet spe-dialogmaxlength\n\
description Text field with character limit and live counter.\n\
	\\$dialog | Add-TextField -Name "${1:title}" -Title "${2:Page Title}" -MaxLength ${3:60} -Tooltip "${4:Keep under ${3:60} chars}"\n\
\n\
snippet spe-dialogmultiline\n\
description Multi-line textarea. Returns string.\n\
	\\$dialog | Add-MultiLineTextField -Name "${1:fieldName}" -Title "${2:Description}" -Lines ${3:3} -Placeholder "${4:Enter text}"\n\
\n\
snippet spe-dialogcheckbox\n\
description Boolean checkbox. Returns bool.\n\
	\\$dialog | Add-Checkbox -Name "${1:fieldName}" -Title "${2:Field Label}" -Value \\$${3:false}\n\
\n\
snippet spe-dialogdate\n\
description Date and time picker. Returns DateTime.\n\
	\\$dialog | Add-DateTimePicker -Name "${1:fieldName}" -Title "${2:Select Date}" -Value ([DateTime]::Now)\n\
\n\
snippet spe-dialogdateonly\n\
description Date-only picker (no time). Returns DateTime.\n\
	\\$dialog | Add-DateTimePicker -Name "${1:fieldName}" -Title "${2:Select Date}" -DateOnly\n\
\n\
snippet spe-dialogitempicker\n\
description Tree-based single item picker. Returns Item. Add -AllowNone for optional selection.\n\
	\\$dialog | Add-ItemPicker -Name "${1:fieldName}" -Title "${2:Select Item}" -Root "${3:/sitecore/content}"\n\
\n\
snippet spe-dialogdroplink\n\
description Flat dropdown, single item selection. Returns Item. Add -AllowNone to allow blank.\n\
	\\$dialog | Add-Droplink -Name "${1:fieldName}" -Title "${2:Select Item}" -Source "${3:DataSource=/sitecore/content}"\n\
\n\
snippet spe-dialogdroplinkoptional\n\
description Flat dropdown with blank first option. Returns Item or \\$null.\n\
	\\$dialog | Add-Droplink -Name "${1:fieldName}" -Title "${2:Select Item}" -Source "${3:DataSource=/sitecore/content}" -Value \\$null -AllowNone -Placeholder "${4:Select...}"\n\
\n\
snippet spe-dialogdroptree\n\
description Tree dropdown, single item selection. Returns Item. Add -AllowNone to allow blank.\n\
	\\$dialog | Add-Droptree -Name "${1:fieldName}" -Title "${2:Choose Item}" -Source "${3:DataSource=/sitecore/content}"\n\
\n\
snippet spe-dialogdroplist\n\
description Flat dropdown, single item selection. Returns Item. Add -AllowNone to allow blank.\n\
	\\$dialog | Add-Droplist -Name "${1:fieldName}" -Title "${2:Choose Item}" -Source "${3:DataSource=/sitecore/content}"\n\
\n\
snippet spe-dialogdroplistoptional\n\
description Flat dropdown with blank first option. Returns Item or \\$null.\n\
	\\$dialog | Add-Droplist -Name "${1:fieldName}" -Title "${2:Choose Item}" -Source "${3:DataSource=/sitecore/content}" -Value \\$null -AllowNone -Placeholder "${4:Select...}"\n\
\n\
snippet spe-dialoggroupeddroplink\n\
description Grouped dropdown by parent folder. Returns Item. Add -AllowNone to allow blank.\n\
	\\$dialog | Add-GroupedDroplink -Name "${1:fieldName}" -Title "${2:Select Item}" -Source "${3:DataSource=/sitecore/templates}"\n\
\n\
snippet spe-dialoggroupeddroplist\n\
description Grouped dropdown by parent folder. Returns Item. Add -AllowNone to allow blank.\n\
	\\$dialog | Add-GroupedDroplist -Name "${1:fieldName}" -Title "${2:Select Item}" -Source "${3:DataSource=/sitecore/templates}"\n\
\n\
snippet spe-dialogdropdown\n\
description Dropdown from custom options hashtable. Returns string.\n\
	\\$dialog | Add-Dropdown -Name "${1:fieldName}" -Title "${2:Field Label}" -Options @{"${3:Option A}" = 1; "${4:Option B}" = 2}\n\
\n\
snippet spe-dialogradio\n\
description Radio button group from options hashtable. Returns string.\n\
	\\$dialog | Add-RadioButtons -Name "${1:fieldName}" -Title "${2:Choose One}" -Options @{"${3:Option A}" = 1; "${4:Option B}" = 2}\n\
\n\
snippet spe-dialogchecklist\n\
description Checklist with select/unselect all. Returns string[].\n\
	\\$dialog | Add-Checklist -Name "${1:fieldName}" -Title "${2:Select Multiple}" -Options @{"${3:Option A}" = 1; "${4:Option B}" = 2; "${5:Option C}" = 4}\n\
\n\
snippet spe-dialogtreelist\n\
description Dual-pane tree list for multi-item selection. Returns Item[].\n\
	\\$dialog | Add-TreeList -Name "${1:fieldName}" -Title "${2:Select Items}" -Source "${3:DataSource=/sitecore/content}"\n\
\n\
snippet spe-dialogmultilist\n\
description Dual-list for multi-item selection. Returns Item[].\n\
	\\$dialog | Add-MultiList -Name "${1:fieldName}" -Title "${2:Select Items}" -Source "${3:DataSource=/sitecore/content}"\n\
\n\
snippet spe-dialoguserpicker\n\
description User account picker. Returns string or string[].\n\
	\\$dialog | Add-UserPicker -Name "${1:fieldName}" -Title "${2:Select User}" -Multiple\n\
\n\
snippet spe-dialogrolepicker\n\
description Security role picker. Returns string or string[].\n\
	\\$dialog | Add-RolePicker -Name "${1:fieldName}" -Title "${2:Select Role}" -Domain "${3:sitecore}"\n\
\n\
snippet spe-dialoguserrolepicker\n\
description Combined user and role picker. Returns string or string[].\n\
	\\$dialog | Add-UserRolePicker -Name "${1:fieldName}" -Title "${2:Select User or Role}" -Multiple\n\
\n\
snippet spe-dialoginfo\n\
description Read-only informational text (not editable).\n\
	\\$dialog | Add-InfoText -Name "${1:fieldName}" -Title "${2:Notice}" -Value "${3:Important information here.}"\n\
\n\
snippet spe-dialogmarquee\n\
description Scrolling marquee text display.\n\
	\\$dialog | Add-Marquee -Name "${1:fieldName}" -Value "${2:Scrolling message...}"\n\
\n\
snippet spe-dialoglink\n\
description Link/URL text input field.\n\
	\\$dialog | Add-LinkField -Name "${1:fieldName}" -Title "${2:URL}" -Placeholder "${3:https://example.com}"\n\
\n\
snippet spe-dialogtristate\n\
description Three-state checkbox: Yes, No, or Not Set. Returns 1, 0, or null.\n\
	\\$dialog | Add-TristateCheckbox -Name "${1:fieldName}" -Title "${2:Override Default}"\n\
\n\
snippet spe-dialogrule\n\
description Sitecore rules condition editor. Returns XML string.\n\
	\\$dialog | Add-RuleField -Name "${1:fieldName}" -Title "${2:Conditions}"\n\
\n\
snippet spe-dialogruleaction\n\
description Sitecore rules condition and action editor. Returns XML string.\n\
	\\$dialog | Add-RuleActionField -Name "${1:fieldName}" -Title "${2:Actions}"\n\
\n\
snippet spe-dialogtabs\n\
description Place fields on different tabs using the -Tab parameter.\n\
	\\$dialog | Add-TextField -Name "${1:field1}" -Title "${2:Field 1}" -Tab "${3:General}"\n\
	\\$dialog | Add-TextField -Name "${4:field2}" -Title "${5:Field 2}" -Tab "${6:Advanced}"\n\
\n\
# SPE SearchBuilder\n\
\n\
snippet spe-searchbuilder\n\
description SearchBuilder starter template with template filter.\n\
	Import-Function -Name SearchBuilder\n\
	\\$search = New-SearchBuilder -Index "${1:sitecore_master_index}" -First ${2:25}\n\
	\\$search | Add-TemplateFilter -Name "${3:Sample Item}"\n\
	\\$results = \\$search | Invoke-Search\n\
	\\$results.Items | ForEach-Object {\n\
	    ${0}\n\
	}\n\
\n\
snippet spe-searchtemplate\n\
description Filter by template name or ID.\n\
	\\$search | Add-TemplateFilter -Name "${1:Template Name}"\n\
\n\
snippet spe-searchfield\n\
description Filter where field contains a keyword.\n\
	\\$search | Add-FieldContains -Field "${1:Title}" -Value "${2:keyword}"\n\
\n\
snippet spe-searchequals\n\
description Filter where field exactly matches a value.\n\
	\\$search | Add-FieldEquals -Field "${1:_language}" -Value "${2:en}"\n\
\n\
snippet spe-searchdate\n\
description Filter by relative date range (e.g. 7d, 3h, 30m).\n\
	\\$search | Add-DateRangeFilter -Field "${1:__Updated}" -Last "${2:7d}"\n\
\n\
snippet spe-searchgroup\n\
description Combine filters with OR/AND logic using a filter group.\n\
	\\$${1:group} = New-SearchFilterGroup -Operation ${2:Or}\n\
	\\$${1:group} | Add-TemplateFilter -Name "${3:Template A}"\n\
	\\$${1:group} | Add-TemplateFilter -Name "${4:Template B}"\n\
	\\$search | Add-SearchFilterGroup -Group \\$${1:group}\n\
\n\
snippet spe-searchfilter\n\
description Generic search filter with explicit Filter type (Equals, Contains, Fuzzy, etc.).\n\
	\\$search | Add-SearchFilter -Field "${1:_templatename}" -Filter "${2:Equals}" -Value "${3:Article}"\n\
\n\
snippet spe-searchreset\n\
description Reset pagination to re-run the same query from the beginning.\n\
	\\$search | Reset-SearchBuilder\n\
\n\
snippet spe-searchfilters\n\
description List all valid filter types (Equals, Contains, Fuzzy, Range, etc.).\n\
	Get-SearchFilter\n\
\n\
snippet spe-searchindexfields\n\
description List available fields in a search index.\n\
	Get-SearchIndexField -Index "${1:sitecore_master_index}" | Where-Object { \\$_.FieldName -like "*${2:template}*" }\n\
\n\
snippet spe-searchpaginate\n\
description SearchBuilder with automatic pagination loop.\n\
	\\$search = New-SearchBuilder -Index "${1:sitecore_master_index}" -First ${2:25}\n\
	\\$search | Add-TemplateFilter -Name "${3:Sample Item}"\n\
	do {\n\
	    \\$results = \\$search | Invoke-Search\n\
	    \\$results.Items | ForEach-Object {\n\
	        ${0}\n\
	    }\n\
	} while (\\$results.HasMore)\n\
\n\
# SPE Reporting\n\
\n\
snippet spe-report\n\
description Show-ListView report with item details and pagination.\n\
	\\$items = Get-ChildItem -Path "${1:master:${2:/sitecore/content}}" -Recurse\n\
	if (\\$items.Count -eq 0) {\n\
	    Show-Alert "No items found matching the criteria."\n\
	} else {\n\
	    \\$props = @{\n\
	        Title = "${3:Report Title}"\n\
	        InfoTitle = "${4:Report details}"\n\
	        InfoDescription = "${5:Description of what this report shows.}"\n\
	        PageSize = 25\n\
	        Property = @(\n\
	            @{Label="Name"; Expression={\\$_.DisplayName}},\n\
	            @{Label="Path"; Expression={\\$_.ItemPath}},\n\
	            @{Label="Updated"; Expression={\\$_.__Updated}},\n\
	            @{Label="Updated by"; Expression={\\$_."__Updated by"}}\n\
	        )\n\
	    }\n\
	    \\$items | Show-ListView @props\n\
	}\n\
	Close-Window\n\
\n\
# SPE Import/Export\n\
\n\
snippet spe-csvimport\n\
description Upload and import a CSV file.\n\
	\\$uploadPath = "${1:temp}"\n\
	\\$filePath = Receive-File -Overwrite -Title "${2:Import Data}" -Path \\$uploadPath\n\
	\\$data = Import-Csv \\$filePath\n\
	foreach (\\$row in \\$data) {\n\
	    ${0}\n\
	}\n\
\n\
snippet spe-csvexport\n\
description Export items to a CSV file.\n\
	\\$items = Get-ChildItem -Path "${1:master:${2:/sitecore/content}}" -Recurse\n\
	\\$items | Select-Object -Property Name, @{Label="Path"; Expression={\\$_.ItemPath}} |\n\
	    Export-Csv -Path "${3:\\$SitecoreDataFolder\\\\export.csv}" -NoTypeInformation\n\
\n\
# SPE Tasks & Logging\n\
\n\
snippet spe-jobprogress\n\
description Loop with Sitecore job progress reporting.\n\
	\\$items = Get-ChildItem -Path "${1:master:${2:/sitecore/content}}" -Recurse\n\
	\\$total = \\$items.Count\n\
	for (\\$i = 0; \\$i -lt \\$total; \\$i++) {\n\
	    \\$item = \\$items[\\$i]\n\
	    [Sitecore.Context]::Job.Status.Processed = \\$i + 1\n\
	    [Sitecore.Context]::Job.Status.Messages.Add("Processing \\$(\\$item.Name)") > \\$null\n\
	    ${0}\n\
	}\n\
\n\
snippet spe-writelog\n\
description Write a message to the SPE log.\n\
	Write-Log "${1:message}"\n\
\n\
snippet spe-importfunction\n\
description Import a shared SPE function by name.\n\
	Import-Function -Name ${1:FunctionName}\n\
\n\
# SPE Advanced Functions\n\
\n\
snippet spe-filter\n\
description Pipeline filter function for Sitecore items.\n\
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
snippet spe-beginprocessend\n\
description Function with begin/process/end pipeline blocks.\n\
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
snippet spe-bulkupdate\n\
description Bulk edit a field across child items.\n\
	\\$items = Get-ChildItem -Path "${1:master:${2:/sitecore/content}}" -Recurse\n\
	foreach (\\$item in \\$items) {\n\
	    \\$item.Editing.BeginEdit()\n\
	    \\$item["${3:FieldName}"] = "${4:value}"\n\
	    \\$item.Editing.EndEdit() > \\$null\n\
	}\n\
\n\
snippet spe-sqlquery\n\
description Execute a SQL query against a Sitecore database.\n\
	Import-Function -Name Invoke-SqlCommand\n\
	\\$connection = [Sitecore.Configuration.Settings]::GetConnectionString("${1:master}")\n\
	\\$query = @"\n\
	SELECT ${2:*} FROM [dbo].[${3:Items}]\n\
	WHERE ${4:condition}\n\
	"@\n\
	\\$results = Invoke-SqlCommand -Connection \\$connection -Query \\$query\n\
\n\
# Advanced Functions\n\
\n\
snippet spe-advfunc\n\
description Advanced function with ShouldProcess and validation.\n\
	function ${1:Verb-Noun} {\n\
	    [CmdletBinding(SupportsShouldProcess)]\n\
	    [OutputType([PSCustomObject])]\n\
	    param(\n\
	        [Parameter(Mandatory, Position = 0, ValueFromPipeline)]\n\
	        [ValidateNotNullOrEmpty()]\n\
	        [string]\\$${2:Name},\n\
	\n\
	        [Parameter()]\n\
	        [ValidateRange(1, 100)]\n\
	        [int]\\$${3:Count} = ${4:10},\n\
	\n\
	        [Parameter()]\n\
	        [ValidateSet("${5:Option1}", "${6:Option2}", "${7:Option3}")]\n\
	        [string]\\$${8:Mode} = "${5:Option1}",\n\
	\n\
	        [Parameter()]\n\
	        [ValidatePattern("${9:^[a-zA-Z]+\\$}")]\n\
	        [string]\\$${10:Filter},\n\
	\n\
	        [Parameter()]\n\
	        [switch]\\$Force\n\
	    )\n\
	\n\
	    begin {\n\
	        ${11}\n\
	    }\n\
	\n\
	    process {\n\
	        if (\\$PSCmdlet.ShouldProcess(\\$${2:Name}, "${12:action}")) {\n\
	            ${0}\n\
	        }\n\
	    }\n\
	\n\
	    end {\n\
	    }\n\
	}\n\
\n\
snippet spe-itemfunc\n\
description Pipeline function for processing Sitecore items.\n\
	function ${1:Verb-SitecoreNoun} {\n\
	    [CmdletBinding()]\n\
	    param(\n\
	        [Parameter(Mandatory, ValueFromPipeline)]\n\
	        [Sitecore.Data.Items.Item]\\$Item,\n\
	\n\
	        [Parameter()]\n\
	        [string]\\$${2:Database} = "master"\n\
	    )\n\
	\n\
	    begin {\n\
	        \\$count = 0\n\
	    }\n\
	\n\
	    process {\n\
	        \\$Item.Editing.BeginEdit()\n\
	        ${0}\n\
	        \\$Item.Editing.EndEdit() > \\$null\n\
	        \\$count++\n\
	    }\n\
	\n\
	    end {\n\
	        Write-Host "Processed \\$count items"\n\
	    }\n\
	}\n\
\n\
snippet spe-paramblock\n\
description CmdletBinding param block with validation attributes.\n\
	[CmdletBinding()]\n\
	param(\n\
	    [Parameter(Mandatory)]\n\
	    [ValidateNotNullOrEmpty()]\n\
	    [string]\\$${1:Name},\n\
	\n\
	    [Parameter()]\n\
	    [ValidateScript({ Test-Path \\$_ })]\n\
	    [string]\\$${2:Path},\n\
	\n\
	    [Parameter()]\n\
	    [switch]\\$${3:Recurse}\n\
	)\n\
';
    exports.scope = "powershell";
  },
);
