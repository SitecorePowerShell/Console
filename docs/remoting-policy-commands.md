# Remoting Policy - Allowed Commands Reference

This document records the review process used to build the command groups offered
in the **Create Remoting Policy** and **Edit Remoting Policy** dialogs. It covers
every compiled cmdlet and every script-library function shipped with SPE,
explains why each was included or excluded, and lists the final grouped result.

## Review methodology

1. Enumerated all 150 compiled C# cmdlets under `src/Spe/Commands/` by scanning
   for `[Cmdlet(...)]` attributes.
2. Enumerated all importable PowerShell functions defined in serialized YAML
   under `serialization/.../Functions/` (approximately 142 function names across
   9 function libraries).
3. Applied the following exclusion filters (in order of priority):
   - **UI / Sheer context** - commands that require a Sitecore Sheer UI context
     (desktop shell, Content Editor, ISE) and will fail or have no effect when
     invoked through the remoting or Web API pipeline.
   - **Client-side remoting** - functions designed to call *into* Sitecore from
     an external PowerShell session (the SPE remoting module). Including them in
     a server-side policy is meaningless.
   - **Indirect script execution** - commands that import or invoke arbitrary
     script items, effectively bypassing the allowed-command list. These are
     placed in a dedicated **Script Execution** group so admins grant them
     deliberately.
   - **Development / internal tooling** - snippet templates, code-formatting
     utilities, integration-point scaffolding, and other functions intended for
     module developers, not API consumers.
   - **Dangerous system operations** - `Restart-Application` and similar
     commands that should never be exposed through a remote API.
4. Remaining commands were organized into 14 groups following a naming
   convention: `Inert:` prefixes read-only groups; `State Change:` prefixes
   groups that mutate content, security, or system state; `Script Execution`
   stands alone as a special escalation group.

---

## Compiled cmdlets (150 total)

### Included in command groups

| Cmdlet | Group | Notes |
|--------|-------|-------|
| Add-BaseTemplate | State Change: Content and Structure | |
| Add-ItemAcl | State Change: Security | |
| Add-ItemVersion | State Change: Content and Structure | |
| Add-PlaceholderSetting | State Change: Presentation and Layout | |
| Add-Rendering | State Change: Presentation and Layout | |
| Add-RoleMember | State Change: Security | |
| Clear-ItemAcl | State Change: Security | |
| Compare-Object | Inert: Data Transformation | Standard PS cmdlet |
| ConvertFrom-CliXml | Inert: Data Transformation | |
| ConvertFrom-Csv | Inert: Data Transformation | Standard PS cmdlet |
| ConvertFrom-Json | Inert: Data Transformation | Standard PS cmdlet |
| ConvertTo-CliXml | Inert: Data Transformation | |
| ConvertTo-Csv | Inert: Data Transformation | Standard PS cmdlet |
| ConvertTo-Json | Inert: Data Transformation | Standard PS cmdlet |
| ConvertTo-Xml | Inert: Data Transformation | Standard PS cmdlet |
| Copy-Item | State Change: Content and Structure | Provider-mapped |
| Disable-User | State Change: Security | |
| Enable-User | State Change: Security | |
| Expand-Token | State Change: Content and Structure | |
| Export-Item | State Change: Packages | |
| Export-Package | State Change: Packages | |
| Export-Role | State Change: Packages | |
| Export-UpdatePackage | State Change: Packages | |
| Export-User | State Change: Packages | |
| Find-Item | Inert: Item Discovery and Inspection | |
| ForEach-Object | Inert: Data Transformation | Standard PS cmdlet |
| Format-Custom | Inert: Data Transformation | Standard PS cmdlet |
| Format-List | Inert: Data Transformation | Standard PS cmdlet |
| Format-Table | Inert: Data Transformation | Standard PS cmdlet |
| Format-Wide | Inert: Data Transformation | Standard PS cmdlet |
| Get-Archive | Inert: System and Security Audit | |
| Get-ArchiveItem | Inert: System and Security Audit | |
| Get-Cache | Inert: System and Security Audit | |
| Get-ChildItem | Inert: Item Discovery and Inspection | Provider-mapped |
| Get-Content | Inert: Variables and Utilities | Standard PS cmdlet |
| Get-Database | Inert: System and Security Audit | |
| Get-Date | Inert: Variables and Utilities | Standard PS cmdlet |
| Get-Domain | Inert: System and Security Audit | |
| Get-Item | Inert: Item Discovery and Inspection | Provider-mapped |
| Get-ItemAcl | Inert: System and Security Audit | |
| Get-ItemClone | Inert: Item Discovery and Inspection | |
| Get-ItemCloneNotification | Inert: Item Discovery and Inspection | |
| Get-ItemField | Inert: Item Discovery and Inspection | |
| Get-ItemReference | Inert: Item Discovery and Inspection | |
| Get-ItemReferrer | Inert: Item Discovery and Inspection | |
| Get-ItemTemplate | Inert: Item Discovery and Inspection | |
| Get-ItemWorkflowEvent | Inert: Item Discovery and Inspection | |
| Get-Layout | Inert: Presentation and Layout | |
| Get-LayoutDevice | Inert: Presentation and Layout | |
| Get-Member | Inert: Variables and Utilities | Standard PS cmdlet |
| Get-Package | Inert: System and Security Audit | |
| Get-PackageItem | Inert: System and Security Audit | |
| Get-PlaceholderSetting | Inert: Presentation and Layout | |
| Get-Preset | Inert: Presentation and Layout | |
| Get-Random | Inert: Variables and Utilities | Standard PS cmdlet |
| Get-Rendering | Inert: Presentation and Layout | |
| Get-RenderingParameter | Inert: Presentation and Layout | |
| Get-Role | Inert: System and Security Audit | |
| Get-RoleMember | Inert: System and Security Audit | |
| Get-ScriptSession | Inert: System and Security Audit | |
| Get-SearchIndex | Inert: System and Security Audit | |
| Get-Session | Inert: System and Security Audit | |
| Get-SitecoreJob | Inert: System and Security Audit | |
| Get-SpeModule | Inert: System and Security Audit | |
| Get-SpeModuleFeatureRoot | Inert: System and Security Audit | |
| Get-TaskSchedule | Inert: System and Security Audit | |
| Get-UpdatePackageDiff | Inert: System and Security Audit | |
| Get-User | Inert: System and Security Audit | |
| Get-UserAgent | Inert: System and Security Audit | |
| Get-Variable | Inert: Variables and Utilities | Standard PS cmdlet |
| Group-Object | Inert: Data Transformation | Standard PS cmdlet |
| Import-Item | State Change: Packages | |
| Import-Role | State Change: Packages | |
| Import-User | State Change: Packages | |
| Initialize-Item | Inert: Item Discovery and Inspection | |
| Initialize-SearchIndex | State Change: Publishing and Indexing | |
| Initialize-SearchIndexItem | State Change: Publishing and Indexing | |
| Install-Package | State Change: Packages | |
| Install-UpdatePackage | State Change: Packages | |
| Lock-Item | State Change: Content and Structure | |
| Measure-Object | Inert: Data Transformation | Standard PS cmdlet |
| Merge-Layout | State Change: Presentation and Layout | |
| Move-Item | State Change: Content and Structure | Provider-mapped |
| New-Domain | State Change: Security | |
| New-ExplicitFileSource | State Change: Packages | |
| New-ExplicitItemSource | State Change: Packages | |
| New-FileSource | State Change: Packages | |
| New-Item | State Change: Content and Structure | Provider-mapped |
| New-ItemAcl | State Change: Security | |
| New-ItemClone | State Change: Content and Structure | |
| New-ItemSource | State Change: Packages | |
| New-ItemWorkflowEvent | State Change: Content and Structure | Creates a workflow event record |
| New-Object | Inert: Variables and Utilities | Standard PS cmdlet |
| New-Package | State Change: Packages | |
| New-PlaceholderSetting | State Change: Presentation and Layout | |
| New-PSObject | Inert: Data Transformation | SPE convenience wrapper |
| New-Rendering | State Change: Presentation and Layout | |
| New-Role | State Change: Security | |
| New-SearchPredicate | Inert: Search | |
| New-SecuritySource | State Change: Packages | |
| New-User | State Change: Security | |
| New-UsingBlock | Inert: Variables and Utilities | |
| Out-Null | Inert: Output and Diagnostics | Standard PS cmdlet |
| Out-String | Inert: Output and Diagnostics | Standard PS cmdlet |
| Protect-Item | State Change: Content and Structure | |
| Publish-Item | State Change: Publishing and Indexing | |
| Receive-ItemCloneNotification | State Change: Content and Structure | |
| Remove-ArchiveItem | State Change: Content and Structure | |
| Remove-BaseTemplate | State Change: Content and Structure | |
| Remove-Domain | State Change: Security | |
| Remove-Item | State Change: Content and Structure | Provider-mapped |
| Remove-ItemVersion | State Change: Content and Structure | |
| Remove-Layout | State Change: Presentation and Layout | |
| Remove-PlaceholderSetting | State Change: Presentation and Layout | |
| Remove-Rendering | State Change: Presentation and Layout | |
| Remove-RenderingParameter | State Change: Presentation and Layout | |
| Remove-Role | State Change: Security | |
| Remove-RoleMember | State Change: Security | |
| Remove-ScriptSession | State Change: Sessions | |
| Remove-SearchIndexItem | State Change: Publishing and Indexing | |
| Remove-Session | State Change: Sessions | |
| Remove-User | State Change: Security | |
| Reset-ItemField | State Change: Content and Structure | |
| Reset-Layout | State Change: Presentation and Layout | |
| Resolve-Path | Inert: Variables and Utilities | Standard PS cmdlet |
| Restore-ArchiveItem | State Change: Content and Structure | |
| Resume-SearchIndex | State Change: Publishing and Indexing | |
| Select-Object | Inert: Data Transformation | Standard PS cmdlet |
| Set-ItemAcl | State Change: Security | |
| Set-ItemTemplate | State Change: Content and Structure | |
| Set-Layout | State Change: Presentation and Layout | |
| Set-Rendering | State Change: Presentation and Layout | |
| Set-RenderingParameter | State Change: Presentation and Layout | |
| Set-User | State Change: Security | |
| Set-UserPassword | State Change: Security | |
| Set-Variable | Inert: Variables and Utilities | Standard PS cmdlet |
| Sort-Object | Inert: Data Transformation | Standard PS cmdlet |
| Start-Sleep | Inert: Variables and Utilities | Standard PS cmdlet |
| Stop-ScriptSession | State Change: Sessions | |
| Stop-SearchIndex | State Change: Publishing and Indexing | |
| Suspend-SearchIndex | State Change: Publishing and Indexing | |
| Switch-Rendering | State Change: Presentation and Layout | |
| Test-Account | Inert: System and Security Audit | |
| Test-BaseTemplate | Inert: Item Discovery and Inspection | |
| Test-ItemAcl | Inert: System and Security Audit | |
| Test-Path | Inert: Variables and Utilities | Standard PS cmdlet |
| Test-Rule | Inert: System and Security Audit | |
| Unlock-Item | State Change: Content and Structure | |
| Unlock-User | State Change: Security | |
| Unprotect-Item | State Change: Content and Structure | |
| Update-ItemReferrer | State Change: Content and Structure | |
| Update-SearchIndexItem | State Change: Publishing and Indexing | |
| Wait-ScriptSession | State Change: Sessions | |
| Where-Object | Inert: Data Transformation | Standard PS cmdlet |
| Write-Debug | Inert: Output and Diagnostics | Standard PS cmdlet |
| Write-Error | Inert: Output and Diagnostics | Standard PS cmdlet |
| Write-Host | Inert: Output and Diagnostics | Standard PS cmdlet |
| Write-Log | Inert: Output and Diagnostics | SPE logging cmdlet |
| Write-Output | Inert: Output and Diagnostics | Standard PS cmdlet |
| Write-Verbose | Inert: Output and Diagnostics | Standard PS cmdlet |
| Write-Warning | Inert: Output and Diagnostics | Standard PS cmdlet |

### Included in Script Execution group (indirect code execution)

| Cmdlet | Reason |
|--------|--------|
| Import-Function | Imports and executes any script library item by name |
| Invoke-Script | Runs any script item by path or ID |
| Invoke-Workflow | Advances workflow state; workflow actions can contain scripts |
| Start-ScriptSession | Creates a new session that can run arbitrary scripts |
| Start-TaskSchedule | Triggers a scheduled task whose definition points to a script item |

### Excluded - UI / Sheer context required

These commands require a Sitecore Sheer UI context (desktop shell, Content
Editor, or ISE). They will fail or have no effect through remoting or Web API.

| Cmdlet | Reason |
|--------|--------|
| Close-Window | Closes a Sheer UI window |
| Invoke-JavaScript | Injects JavaScript into a Sheer UI page |
| Login-User | Performs an interactive Sitecore login; remoting uses API key auth |
| Logout-User | Ends an interactive Sitecore session |
| Out-Download | Streams a file download to the browser |
| Read-Variable | Opens the Sheer UI variable-entry dialog |
| Receive-File | Opens a Sheer UI file-upload dialog |
| Receive-ScriptSession | Retrieves results in a Sheer UI polling pattern |
| Send-File | Streams a file download to the browser (Sheer) |
| Send-SheerMessage | Sends a raw Sheer message to the UI pipeline |
| Set-HostProperty | Sets properties on the ISE/Console host window |
| Show-Alert | Displays a Sheer alert dialog |
| Show-Application | Launches a Sitecore desktop application |
| Show-Confirm | Displays a Sheer confirmation dialog |
| Show-FieldEditor | Opens the Sitecore field editor dialog |
| Show-Input | Displays a Sheer text-input dialog |
| Show-ListView | Displays results in the ISE ListView panel |
| Show-ModalDialog | Opens an arbitrary Sheer modal dialog |
| Show-Result | Displays results in a Sheer output window |
| Show-YesNoCancel | Displays a Sheer three-button dialog |
| Update-ListView | Updates an existing ISE ListView panel |

### Excluded - dangerous system operations

| Cmdlet | Reason |
|--------|--------|
| Restart-Application | Recycles the application pool; should never be exposed remotely |

---

## Script-library functions (142 total across 9 libraries)

### Included in command groups

| Function | Source library | Group | Notes |
|----------|---------------|-------|-------|
| Add-DateRangeFilter | Search Builder | Inert: Search | |
| Add-FieldContains | Search Builder | Inert: Search | |
| Add-FieldEquals | Search Builder | Inert: Search | |
| Add-SearchFilter | Search Builder | Inert: Search | |
| Add-SearchFilterGroup | Search Builder | Inert: Search | |
| Add-TemplateFilter | Search Builder | Inert: Search | |
| Clear-Archive | Core Platform | State Change: Content and Structure | Clears Sitecore recycle bin / archive |
| Compress-Archive | Core Platform | Inert: Variables and Utilities | Creates zip archives |
| Expand-Archive | Core Platform | Inert: Variables and Utilities | Extracts zip archives |
| Get-LockedChildItem | Core Platform | Inert: Item Discovery and Inspection | Queries locked descendants |
| Get-SearchFilter | Search Builder | Inert: Search | |
| Get-SearchIndexField | Search Builder | Inert: Search | |
| Invoke-Search | Search Builder | Inert: Search | Executes a built search query, not arbitrary code |
| Invoke-SqlCommand | Core Platform | Script Execution | Runs arbitrary SQL against the database |
| New-PackagePostStep | Core Platform | State Change: Packages | |
| New-SearchBuilder | Search Builder | Inert: Search | |
| New-SearchFilterGroup | Search Builder | Inert: Search | |
| Resolve-Error | Core Platform | Inert: Output and Diagnostics | |
| Reset-SearchBuilder | Search Builder | Inert: Search | |

### Excluded - UI / dialog functions

| Function | Source library | Reason |
|----------|---------------|--------|
| New-DialogBuilder | Dialog Builder | Builds Sheer UI dialogs |
| Add-DialogField | Dialog Builder | Adds fields to Sheer UI dialogs |
| Add-Checkbox | Dialog Builder | Dialog field type |
| Add-Checklist | Dialog Builder | Dialog field type |
| Add-DateTimePicker | Dialog Builder | Dialog field type |
| Add-Dropdown | Dialog Builder | Dialog field type |
| Add-Droplink | Dialog Builder | Dialog field type |
| Add-Droplist | Dialog Builder | Dialog field type |
| Add-Droptree | Dialog Builder | Dialog field type |
| Add-GroupedDroplink | Dialog Builder | Dialog field type |
| Add-GroupedDroplist | Dialog Builder | Dialog field type |
| Add-InfoText | Dialog Builder | Dialog field type |
| Add-ItemPicker | Dialog Builder | Dialog field type |
| Add-LinkField | Dialog Builder | Dialog field type |
| Add-Marquee | Dialog Builder | Dialog field type |
| Add-MultiLineTextField | Dialog Builder | Dialog field type |
| Add-MultiList | Dialog Builder | Dialog field type |
| Add-RadioButtons | Dialog Builder | Dialog field type |
| Add-RolePicker | Dialog Builder | Dialog field type |
| Add-RuleActionField | Dialog Builder | Dialog field type |
| Add-RuleField | Dialog Builder | Dialog field type |
| Add-TextField | Dialog Builder | Dialog field type |
| Add-TreeList | Dialog Builder | Dialog field type |
| Add-TristateCheckbox | Dialog Builder | Dialog field type |
| Add-UserPicker | Dialog Builder | Dialog field type |
| Add-UserRolePicker | Dialog Builder | Dialog field type |
| ConvertTo-DialogBuilderJson | Dialog Builder | Dialog serialization |
| Copy-DialogBuilder | Dialog Builder | Dialog duplication |
| Invoke-Dialog | Dialog Builder | Opens the Sheer dialog |
| Remove-DialogField | Dialog Builder | Dialog field removal |
| Show-FieldDebugInfo | Dialog Builder | Debug output in Sheer |
| Test-DialogBuilder | Dialog Builder | Dialog testing |
| Edit-TaskSchedule | Task Management | Opens a Sheer dialog to edit schedules |
| Show-SearchResultDetails | Authorable Reports | Opens a Sheer results dialog |
| Get-ReportQuery | Authorable Reports | UI report infrastructure |
| Get-ReportRule | Authorable Reports | UI report infrastructure |
| Render-ScriptInvoker | Core Platform | Report rendering (UI) |
| Render-ItemField | Core Platform | Report rendering (UI) |
| Render-PercentValue | Core Platform | Report rendering (UI) |

### Excluded - client-side remoting functions

These are part of the SPE PowerShell remoting module and run on the *client*
machine, not inside Sitecore.

| Function | Source library | Reason |
|----------|---------------|--------|
| Set-SitecoreConfiguration | Remoting | Client-side session setup |
| Invoke-SitecoreScript | Remoting | Client-side script invocation |
| Upload-SitecoreFile | Remoting | Client-side upload |
| Download-SitecoreFile | Remoting | Client-side download |
| New-ScriptSession | Remoting2 | Client-side session creation |
| Invoke-RemoteScript | Remoting2 | Client-side script invocation |
| Send-MediaItem | Remoting2 | Client-side media upload |
| Receive-MediaItem | Remoting2 | Client-side media download |
| Get-ImageExtension | Remoting2 | Helper for client-side media ops |
| ConvertFrom-CliXml | Remoting2 | Duplicate of compiled cmdlet |
| ConvertTo-CliXml | Remoting2 | Duplicate of compiled cmdlet |

### Excluded - development / internal tooling

| Function | Source library | Reason |
|----------|---------------|--------|
| CreatePathRecursively | Core Platform | Integration-point scaffolding |
| CreateIntegrationPoint | Core Platform | Integration-point scaffolding |
| Invoke-ApiScript | Core Platform | Internal API dispatch, not user-facing |
| Purge-EmptyLibrary | Core Platform | Module maintenance tool |
| Verb-Noun | Snippets | Template/snippet placeholder |
| Do-Something | Snippets | Template/snippet placeholder |

### Excluded - DTW code formatter (38 functions)

The entire `Edit-DTWCleanScript` library is a code-formatting tool for ISE use.
None of these functions are relevant to remoting.

`Initialize-Module`, `Initialize-ProcessVariables`, `Set-LookupTableValues`,
`Get-ValidCommandNames`, `Get-ValidCommandParameterNames`,
`Get-ValidAttributeNames`, `Get-ValidMemberNames`, `Get-ValidVariableNames`,
`Lookup-ValidCommandName`, `Lookup-ValidCommandParameterName`,
`Lookup-ValidAttributeName`, `Lookup-ValidMemberName`,
`Lookup-ValidVariableName`, `Add-StringContentToDestinationFileStreamWriter`,
`Copy-ArrayContentFromSourceArrayToDestinationFileStreamWriter`,
`Tokenize-SourceScriptContent`, `Migrate-SourceContentToDestinationStream`,
`Write-TokenContentByType`, `Write-TokenContent_Attribute`,
`Write-TokenContent_Command`, `Write-TokenContent_CommandArgument`,
`Write-TokenContent_CommandParameter`, `Write-TokenContent_Comment`,
`Write-TokenContent_GroupEnd`, `Write-TokenContent_GroupStart`,
`Write-TokenContent_KeyWord`, `Write-TokenContent_LoopLabel`,
`Write-TokenContent_LineContinuation`, `Write-TokenContent_Member`,
`Write-TokenContent_NewLine`, `Write-TokenContent_Number`,
`Write-TokenContent_Operator`, `Write-TokenContent_Position`,
`Write-TokenContent_StatementSeparator`, `Write-TokenContent_String`,
`Write-TokenContent_Type`, `Write-TokenContent_Variable`,
`Write-TokenContent_Unknown`, `Test-AddSpaceFollowingToken`,
`Edit-DTWCleanScriptInMemory`

### Excluded - Package Generator internal helpers (17 functions)

These are internal helper functions used by the Package Generator UI wizard.
They are not standalone commands.

`Test-LinkedOption`, `AddItems`, `ProcessItemWithLinks`,
`IsSystemTemplateItem`, `ProcessFieldTemplateItem`, `ProcessTemplateItem`,
`ProcessWorkflowItem`, `ProcessItem`, `ProcessItemWithDescendants`,
`Add-ItemToPackage`, `Get-UniqueLinks`, `Get-UniqueReferrers`,
`Get-UniqueReferences`, `Get-OptionsForItems`, `Filter-ByPathContains`,
`Get-OtherItems`, `Get-ChildrenToInclude`, `Get-LinkedItems`,
`Get-SelectedLinks`

### Excluded - Task Management helpers

| Function | Source library | Reason |
|----------|---------------|--------|
| Parse-TaskSchedule | Task Management | Internal parser for Edit-TaskSchedule dialog |
| Format-TaskSchedule | Task Management | Internal formatter for dialog |
| Format-TaskScheduleDate | Task Management | Internal formatter for dialog |
| Format-TaskScheduleDay | Task Management | Internal formatter for dialog |

### Excluded - Excel functions

| Function | Source library | Reason |
|----------|---------------|--------|
| New-XlsxWorkbook | Core Platform (BaseXlsx) | Depends on EPPlus library; not guaranteed available |
| Add-XlsxWorksheet | Core Platform (BaseXlsx) | Same dependency |
| Write-WorksheetHeader | Core Platform (BaseXlsx) | Same dependency |
| Export-Worksheet | Core Platform (BaseXlsx) | Same dependency |
| ConvertTo-Xlsx | Core Platform | Same dependency |
| Export-Xlsx | Core Platform | Same dependency |
| Get-DTWFileEncoding | Core Platform | Development utility for file encoding detection |

---

## Previously listed commands not found in codebase

The following commands appeared in the original command groups but do not exist
as compiled cmdlets or script-library functions. They were removed.

| Command | Notes |
|---------|-------|
| Get-ItemByUri | Not found in compiled cmdlets or functions |
| Set-ItemField | Not found; field editing is done via `$item.Editing.BeginEdit()` or the provider |
| Clear-ItemField | Not found; use `Reset-ItemField` instead |

---

## Final command groups

### Inert: Item Discovery and Inspection

- Find-Item
- Get-ChildItem
- Get-Item
- Get-ItemClone
- Get-ItemCloneNotification
- Get-ItemField
- Get-ItemReference
- Get-ItemReferrer
- Get-ItemTemplate
- Get-ItemWorkflowEvent
- Get-LockedChildItem
- Initialize-Item
- Test-BaseTemplate

### Inert: Presentation and Layout

- Get-Layout
- Get-LayoutDevice
- Get-PlaceholderSetting
- Get-Preset
- Get-Rendering
- Get-RenderingParameter

### Inert: System and Security Audit

- Get-Archive
- Get-ArchiveItem
- Get-Cache
- Get-Database
- Get-Domain
- Get-ItemAcl
- Get-Package
- Get-PackageItem
- Get-Role
- Get-RoleMember
- Get-ScriptSession
- Get-SearchIndex
- Get-Session
- Get-SitecoreJob
- Get-SpeModule
- Get-SpeModuleFeatureRoot
- Get-TaskSchedule
- Get-UpdatePackageDiff
- Get-User
- Get-UserAgent
- Test-Account
- Test-ItemAcl
- Test-Rule

### Inert: Search

- Add-DateRangeFilter
- Add-FieldContains
- Add-FieldEquals
- Add-SearchFilter
- Add-SearchFilterGroup
- Add-TemplateFilter
- Get-SearchFilter
- Get-SearchIndexField
- Invoke-Search
- New-SearchBuilder
- New-SearchFilterGroup
- New-SearchPredicate
- Reset-SearchBuilder

### Inert: Data Transformation

- Compare-Object
- ConvertFrom-CliXml
- ConvertFrom-Csv
- ConvertFrom-Json
- ConvertTo-CliXml
- ConvertTo-Csv
- ConvertTo-Json
- ConvertTo-Xml
- ForEach-Object
- Format-Custom
- Format-List
- Format-Table
- Format-Wide
- Group-Object
- Measure-Object
- New-PSObject
- Select-Object
- Sort-Object
- Where-Object

### Inert: Output and Diagnostics

- Out-Null
- Out-String
- Resolve-Error
- Write-Debug
- Write-Error
- Write-Host
- Write-Log
- Write-Output
- Write-Verbose
- Write-Warning

### Inert: Variables and Utilities

- Compress-Archive
- Expand-Archive
- Get-Content
- Get-Date
- Get-Member
- Get-Random
- Get-Variable
- New-Object
- New-UsingBlock
- Resolve-Path
- Set-Variable
- Start-Sleep
- Test-Path

### State Change: Content and Structure

- Add-BaseTemplate
- Add-ItemVersion
- Clear-Archive
- Copy-Item
- Expand-Token
- Lock-Item
- Move-Item
- New-Item
- New-ItemClone
- New-ItemWorkflowEvent
- Protect-Item
- Receive-ItemCloneNotification
- Remove-ArchiveItem
- Remove-BaseTemplate
- Remove-Item
- Remove-ItemVersion
- Reset-ItemField
- Restore-ArchiveItem
- Set-ItemTemplate
- Unlock-Item
- Unprotect-Item
- Update-ItemReferrer

### State Change: Presentation and Layout

- Add-PlaceholderSetting
- Add-Rendering
- Merge-Layout
- New-PlaceholderSetting
- New-Rendering
- Remove-Layout
- Remove-PlaceholderSetting
- Remove-Rendering
- Remove-RenderingParameter
- Reset-Layout
- Set-Layout
- Set-Rendering
- Set-RenderingParameter
- Switch-Rendering

### State Change: Security

- Add-ItemAcl
- Add-RoleMember
- Clear-ItemAcl
- Disable-User
- Enable-User
- New-Domain
- New-ItemAcl
- New-Role
- New-User
- Remove-Domain
- Remove-Role
- Remove-RoleMember
- Remove-User
- Set-ItemAcl
- Set-User
- Set-UserPassword
- Unlock-User

### State Change: Publishing and Indexing

- Initialize-SearchIndex
- Initialize-SearchIndexItem
- Publish-Item
- Remove-SearchIndexItem
- Resume-SearchIndex
- Stop-SearchIndex
- Suspend-SearchIndex
- Update-SearchIndexItem

### State Change: Packages

- Export-Item
- Export-Package
- Export-Role
- Export-UpdatePackage
- Export-User
- Import-Item
- Import-Role
- Import-User
- Install-Package
- Install-UpdatePackage
- New-ExplicitFileSource
- New-ExplicitItemSource
- New-FileSource
- New-ItemSource
- New-Package
- New-PackagePostStep
- New-SecuritySource

### State Change: Sessions

- Remove-ScriptSession
- Remove-Session
- Stop-ScriptSession
- Wait-ScriptSession

### Script Execution

Commands in this group can execute arbitrary scripts or SQL, effectively
bypassing the allowed-command list. Enable only when the policy holder is
trusted to run unrestricted code.

- Import-Function
- Invoke-Script
- Invoke-SqlCommand
- Invoke-Workflow
- Start-ScriptSession
- Start-TaskSchedule
