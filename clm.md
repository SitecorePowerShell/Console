# Constrained Language Mode Profiles for SPE Remoting Security

## Summary

This issue proposes predefined constrained language mode profiles and configuration guidance for securing SPE remoting endpoints. Based on a comprehensive audit of all 291 PowerShell scripts serialized in YAML across the `release/9.0` branch, this document catalogs every .NET API, Sitecore library, function, and cmdlet in use, then recommends tiered restriction profiles, function trust models, and a hybrid config/item-based management system.

**Labels:** `security`, `enhancement`, `design`, `remoting`

---

## Table of Contents

1. [API & Cmdlet Catalog](#1-api--cmdlet-catalog)
2. [Proposed Restriction Profiles](#2-proposed-restriction-profiles)
3. [Function Trust Model](#3-function-trust-model)
4. [Nonstandard Function Names & AST Analysis](#4-nonstandard-function-names--ast-analysis)
5. [User Script Remoting Access](#5-user-script-remoting-access)
6. [Hybrid Configuration Model](#6-hybrid-configuration-model)
7. [Audit Logging](#7-audit-logging)
8. [Migration & Backward Compatibility](#8-migration--backward-compatibility)
9. [Implementation Considerations](#9-implementation-considerations)

---

## 1. API & Cmdlet Catalog

### 1.1 .NET Framework APIs Found in Serialized Scripts

#### System.Data.SqlClient (SQL Database Access)

- `[System.Data.SqlClient.SQLConnection]`
- `[System.Data.SqlClient.SqlCommand]`
- `[System.Data.CommandType]::Text`
- `[System.Data.DataSet]`, `[System.Data.DataTable]`, `[System.Data.DataRow]`
- **Used in:** `Invoke-SqlCommand.yml`, security audit reports

#### System.IO (File System & Compression)

- `[System.IO.Compression.ZipFile]::ExtractToDirectory()`
- `[System.IO.DirectoryInfo]`
- `[System.IO.Directory]::Exists()`, `[System.IO.Directory]::GetFiles()`
- `[System.IO.StringReader]`
- `[System.IO.File]::ReadAllBytes()`, `[System.IO.File]::WriteAllBytes()`
- `[System.IO.Packaging.Package]::Open()`
- **Used in:** `Expand-Archive.yml`, `Remoting.yml`, `ConvertTo-Xlsx.yml`, media library scripts

#### System.Text (Encoding & Regex)

- `[System.Text.StringBuilder]`
- `[System.Text.Encoding]::UTF8`
- `[System.Text.RegularExpressions.RegexOptions]::IgnoreCase`
- **Used in:** `Remoting.yml`, Rules Based Report, security audit reports

#### System.Xml (XML Processing)

- `[System.Xml.XmlWriter]::Create()`
- `[System.Xml.XmlTextReader]`
- `[System.Xml.XmlDocument]`
- **Used in:** `Remoting.yml`, `ConvertTo-Xlsx.yml`

#### System.Web (HTTP & Security)

- `[System.Web.Security.Membership]::GetUser()`
- `[System.Web.HttpUtility]::UrlEncode()`
- **Used in:** `Enforce password expiration.yml`, Authorable Reports refresh

#### System.Reflection (Private Member Access)

- `[PSObject].Assembly.GetType("System.Management.Automation.Serializer")`
- `[PSObject].Assembly.GetType("System.Management.Automation.Deserializer")`
- `[System.Reflection.BindingFlags]"nonpublic,instance"`
- `$type.GetConstructor("instance,nonpublic", ...)`, `$type.GetMethod(...)`
- **Used in:** `Remoting.yml`, `Remoting2.yml` (PowerShell serialization internals)

#### System.Management.Automation

- `[System.Management.Automation.PSObject]`
- `[scriptblock]::Create()` (dynamic code generation)
- **Used in:** `ConvertTo-Xlsx.yml`, `Rules Based Report.yml`

#### Other System Types

- `[DateTime]::Now`, `[DateTime]::Today`, `[timespan]`
- `[guid]::Empty`
- `[int]`, `[string]`, `[bool]`, `[hashtable]`, `[array]`, `[xml]`, `[regex]`
- `New-Object System.Random`
- **576 total type accelerator/cast instances** across all scripts

### 1.2 Sitecore APIs Found in Serialized Scripts

#### Sitecore.Data (Item & Field Management)

- `[Sitecore.Data.Items.Item]` — type checking and casting
- `[Sitecore.Data.Managers.TemplateManager]::GetTemplate()`
- `[Sitecore.Data.Fields.FieldTypeManager]::GetField()`
- `[Sitecore.Data.Fields.ImageField]` — type casting
- `[Sitecore.Data.ID]`
- `New-Object Sitecore.Data.Items.TemplateItem`
- `[Sitecore.ItemIDs]::ContentRoot`

#### Sitecore.Configuration

- `[Sitecore.Configuration.Settings]::DefaultBaseTemplate`
- `[Sitecore.Configuration.Settings]::WallpapersPath`

#### Sitecore.ContentSearch

- `New-Object Sitecore.ContentSearch.SearchTypes.SearchResultItem`
- `[Sitecore.ContentSearch.Utilities.IndexHealthHelper]::GetIndexNumberOfDocuments()`

#### Sitecore.Jobs

- `[Sitecore.Jobs.JobManager]::GetJobs()`

#### Sitecore.IO

- `[Sitecore.IO.FileUtil]::MapPath()`
- `[Sitecore.IO.FileUtil]::UnmapPath()`

#### Sitecore.Web

- `[Sitecore.Web.WebUtil]::Redirect()`

#### Spe.Core (SPE Internal)

- `[Spe.Core.Modules.ModuleManager]::GetFeatureRoots()`
- `[Spe.Core.Modules.IntegrationPoints]::ReportStartMenuFeature`

### 1.3 SPE-Specific Cmdlets

#### Item Operations

- `Get-Item`, `Set-Item`, `Remove-Item`, `New-Item`, `Get-ChildItem`
- `Find-Item` (content search)

#### User & Security

- `Get-User`, `Get-Role`

#### UI & Dialogs

- `Read-Variable`, `Close-Window`, `Show-Alert`, `Show-ListView`

#### Database

- `Get-Database`

#### Rules

- `Test-Rule`

#### Custom (from Import-Function)

- `Import-Function` — loads functions from Script Library
- `Invoke-SqlCommand` — SQL database operations
- `New-DialogBuilder`, `Add-DialogField`, `Show-Dialog` — UI framework
- `Expand-Archive`, `Compress-Archive` — ZIP operations
- `ConvertTo-Xlsx`, `Export-Xlsx` — Excel generation
- `Edit-TaskSchedule` — task management

### 1.4 Standard PowerShell Cmdlets

- `New-Object`, `Add-Type`, `Get-Variable`, `Set-Variable`
- `Write-Verbose`, `Write-Warning`, `Write-Error`, `Write-Host`
- `Select-Object`, `Where-Object`, `ForEach-Object`, `Sort-Object`
- `Test-Path`, `Resolve-Path`, `Convert-Path`
- `Get-Content`, `Set-Content`

### 1.5 Assembly Loading

- `Add-Type -AssemblyName System.IO.Compression`
- `Add-Type -AssemblyName System.IO.Compression.FileSystem`

### 1.6 Dangerous Patterns Identified

| Pattern                                | Location                        | Risk                    |
| -------------------------------------- | ------------------------------- | ----------------------- |
| Reflection with nonpublic BindingFlags | Remoting.yml, Remoting2.yml     | Bypasses access control |
| `[scriptblock]::Create()`              | Rules Based Report.yml          | Dynamic code execution  |
| `Add-Type -AssemblyName`               | Expand-Archive.yml              | Assembly loading        |
| Direct SQL via SqlClient               | Invoke-SqlCommand.yml           | Database access         |
| `[System.Web.Security.Membership]`     | Enforce password expiration.yml | Identity manipulation   |

**Not found (good):** `Invoke-Expression`, `Start-Process`, `Add-Type -TypeDefinition`, `Add-Type -Path`

---

## 2. Proposed Restriction Profiles

Based on the API catalog, we recommend **four tiered profiles** that combine PowerShell's native `LanguageMode` with SPE's existing `commandRestrictions` system. All profiles are **opt-in** (fully backward-compatible).

### Profile: `unrestricted` (Default — Current Behavior)

- **LanguageMode:** FullLanguage
- **Command Restrictions:** None
- **Use case:** Trusted admin scripts, development environments
- **Note:** This is the current default. No behavior change.

### Profile: `read-only-sitecore`

- **LanguageMode:** ConstrainedLanguage
- **Blocked Cmdlets:**
  - `Set-Item`, `Remove-Item`, `New-Item` (Sitecore provider)
  - `Move-Item`, `Copy-Item`, `Rename-Item`
  - `Publish-Item`, `Protect-Item`, `Unprotect-Item`
  - `Install-Package`, `New-Package`
  - `Set-Layout`, `Add-Rendering`, `Remove-Rendering`
- **Allowed Cmdlets:**
  - `Get-Item`, `Get-ChildItem`, `Find-Item`, `Get-Database`
  - `Get-User`, `Get-Role`, `Get-Domain`
  - `Read-Variable`, `Show-ListView`, `Show-Alert`
  - `Test-Rule`, `Test-Path`
  - All formatting/output cmdlets
- **Allowed .NET Types:** Primitive types only (`[int]`, `[string]`, `[bool]`, `[DateTime]`, `[guid]`, `[regex]`)
- **Use case:** Reporting integrations, dashboards, monitoring

### Profile: `read-only-full`

Extends `read-only-sitecore` with additional restrictions:

- **Additional Blocked Cmdlets:**
  - `Invoke-SqlCommand` (or restrict to SELECT-only — see section 2.1)
  - `Set-Content`, `Out-File`, `Export-Csv`, `Export-Clixml`
  - `Add-Type`
  - `Compress-Archive`, `Expand-Archive`
  - `Send-SheerMessage`
- **Blocked .NET Types (via CLM):** All non-primitive .NET types blocked by ConstrainedLanguage
- **Blocked Patterns:**
  - `[System.IO.File]::Write*`, `[System.IO.File]::Delete`
  - `[System.Data.SqlClient.*]` (direct SQL access)
- **Use case:** Untrusted external consumers, third-party integrations

### Profile: `content-editor`

A middle ground between unrestricted and read-only:

- **LanguageMode:** ConstrainedLanguage
- **Allowed Cmdlets (allowlist mode):**
  - All read cmdlets from `read-only-sitecore`
  - `Set-Item` (Sitecore content fields only)
  - `Publish-Item`
  - `New-Item` (content items only, not script items)
  - `Lock-Item`, `Unlock-Item`
- **Blocked Cmdlets:**
  - `Remove-Item` (deletions require elevated profile)
  - `Install-Package`, `New-Package`
  - `Add-Type`, `Invoke-SqlCommand`
  - All Script Library management cmdlets
- **Use case:** Content management APIs, headless CMS integrations

### 2.1 SQL Access in Read-Only Profiles

For `Invoke-SqlCommand` specifically, recommend a **SQL statement validator** that:

1. Parses the SQL string for statement type
2. Allows: `SELECT`, `WITH ... SELECT`, `EXEC` for known read-only stored procedures
3. Blocks: `INSERT`, `UPDATE`, `DELETE`, `DROP`, `ALTER`, `CREATE`, `TRUNCATE`, `EXEC` (arbitrary)
4. Configuration: An allowlist of stored procedure names that are considered read-only

```xml
<sqlRestrictions mode="read-only">
  <allowedStatements>
    <statement>SELECT</statement>
  </allowedStatements>
  <allowedProcedures>
    <procedure>sp_helpindex</procedure>
    <procedure>sp_columns</procedure>
  </allowedProcedures>
</sqlRestrictions>
```

### 2.2 Add-Type Recommendations

For constrained profiles, recommend **blocking `Add-Type` entirely** with these exceptions configurable via allowlist:

- `Add-Type -AssemblyName System.IO.Compression` — needed by `Expand-Archive` function
- `Add-Type -AssemblyName System.IO.Compression.FileSystem` — needed by `Expand-Archive` function

Always block:

- `Add-Type -TypeDefinition` (compiles arbitrary C# code)
- `Add-Type -Path` (loads arbitrary DLLs)
- `Add-Type -MemberDefinition` (P/Invoke definitions)

```xml
<assemblyRestrictions>
  <allowedAssemblies>
    <assembly>System.IO.Compression</assembly>
    <assembly>System.IO.Compression.FileSystem</assembly>
  </allowedAssemblies>
</assemblyRestrictions>
```

### 2.3 Dynamic Evaluation (scriptblock::Create)

Recommend: **Allow `[scriptblock]::Create()` in CLM** because PowerShell's ConstrainedLanguage mode already restricts what scriptblocks can do — they inherit the language mode of the session. Block `Invoke-Expression` explicitly in all constrained profiles.

### 2.4 File System Recommendations

For `read-only-sitecore`: Allow file reads everywhere, block file writes.
For `read-only-full`: Block all direct file system .NET calls; allow only `Get-Content`, `Test-Path`, `Resolve-Path` cmdlets.

```xml
<fileSystemRestrictions mode="read-only">
  <allowedCommands>
    <command>Get-Content</command>
    <command>Test-Path</command>
    <command>Resolve-Path</command>
    <command>Get-ChildItem</command>
  </allowedCommands>
</fileSystemRestrictions>
```

---

## 3. Function Trust Model

### Problem

SPE scripts heavily use `Import-Function` to load user-curated functions from the Script Library (e.g., `DialogBuilder`, `Invoke-SqlCommand`, `Remoting`). These functions often require .NET type access that ConstrainedLanguage blocks. A blanket CLM enforcement would break most SPE built-in functionality.

### Recommendation: Trust-Based Function Elevation

Implement a **function trust registry** where specific functions can be marked as "trusted" and allowed to execute with elevated privileges even when the calling script runs in a constrained mode.

#### Trust Levels

1. **Untrusted (default)** — Function runs under the caller's language mode and command restrictions. No special privileges.
2. **Trusted** — Function can use .NET types and reflection even when called from a constrained session. The function body is validated at registration time.
3. **System** — Reserved for SPE internal functions (Remoting.yml, Remoting2.yml). Can access private members via reflection. Cannot be assigned via Sitecore items — config only.

#### Trust Assignment

Functions are trusted based on their **Script Library path** combined with an explicit trust marker:

```xml
<trustedFunctions>
  <!-- System-level trust (reflection allowed) -->
  <function path="/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Platform/Functions/*"
            trust="System" />
  <!-- Trusted (CLM bypass, no reflection) -->
  <function path="/sitecore/system/Modules/PowerShell/Script Library/SPE/Extensions/Dialog Builder/Functions/*"
            trust="Trusted" />
  <function path="/sitecore/system/Modules/PowerShell/Script Library/SPE/Extensions/Authorable Reports/Functions/*"
            trust="Trusted" />
</trustedFunctions>
```

#### How It Works

1. When `Import-Function` is called, the engine checks the function's Script Library path against the trust registry
2. If trusted, the function's scriptblock is wrapped in a temporary `FullLanguage` scope
3. When the function returns, execution resumes in the caller's constrained mode
4. The function's script body is parsed via AST at import time to verify it hasn't been tampered with (hash validation)

#### Security Boundary

- Trusted functions can access .NET types but **cannot export variables** back to the constrained scope that would bypass restrictions
- Return values from trusted functions are subject to the caller's type restrictions
- Trusted functions cannot call `Set-Variable -Scope` to inject into parent scopes

---

## 4. Nonstandard Function Names & AST Analysis

### Problem

Some user-defined functions use nonstandard names that don't follow PowerShell's `Verb-Noun` convention:

- `Render-ImageItemPath` (unapproved verb "Render")
- `Setup-PackageGenerator` (unapproved verb "Setup")
- A function named `Get-Data` appears read-only but could internally call `Remove-Item`

Function names alone are unreliable indicators of whether a function is read-only or has side effects.

### Recommendation: AST-Based Static Analysis

Extend the existing `ScriptValidator.cs` (which already parses scripts into AST) to perform **deep analysis of function bodies**:

#### Analysis Algorithm

```
For each function in script:
  1. Parse function body into AST
  2. Extract all CommandAst nodes (cmdlet/function calls)
  3. Extract all InvokeMemberExpressionAst nodes (.NET method calls)
  4. Extract all TypeExpressionAst nodes (.NET type references)
  5. Compare against the active profile's restrictions
  6. If ANY blocked command/type is found → function is NOT read-only
  7. Recursively analyze any Import-Function calls within the function
```

#### Caching

- AST analysis results should be cached per script item version (keyed by `__Revision` field)
- Cache invalidated when script content changes
- Cache stored in `HttpRuntime.Cache` with configurable expiration (default: `Spe.WebApiCacheExpirationSecs`)

#### Classification Output

Each function gets classified as:

- **ReadOnly** — No write cmdlets, no blocked .NET types, no dynamic evaluation
- **WriteCapable** — Contains write operations but no dangerous patterns
- **Elevated** — Contains reflection, Add-Type, or dynamic evaluation
- **Unknown** — Contains `Import-Function` calls to unanalyzed functions (conservative: treated as Elevated)

#### Example: Nonstandard Name Analysis

```
Function: Render-ImageItemPath
  - Verb "Render" is nonstandard
  - AST analysis reveals: only uses Get-Item, property access, string formatting
  - Classification: ReadOnly ✓
  - Allowed in read-only profiles despite nonstandard name

Function: Setup-PackageGenerator
  - Verb "Setup" is nonstandard
  - AST analysis reveals: uses New-Item, Set-Item, New-Package
  - Classification: WriteCapable ✗
  - Blocked in read-only profiles
```

---

## 5. User Script Remoting Access

### Problem

Users create custom scripts in the Script Library that they want accessible via SPE remoting. However, the command restriction system (blocklist/allowlist) operates at the cmdlet level, not the script level. A user's custom function name won't be in any predefined allowlist.

### Recommendation: Multi-Layer Access Control

Combine three mechanisms:

#### Layer 1: Script Library Path-Based Access

Define which Script Library paths are exposed to remoting:

```xml
<remotingScripts>
  <!-- SPE built-in scripts always available -->
  <include path="/sitecore/system/Modules/PowerShell/Script Library/SPE/**" />
  <!-- Custom module scripts opted-in by admin -->
  <include path="/sitecore/system/Modules/PowerShell/Script Library/Custom Reports/**" />
  <!-- Explicitly exclude sensitive paths -->
  <exclude path="/sitecore/system/Modules/PowerShell/Script Library/SPE/Maintenance/**" />
</remotingScripts>
```

#### Layer 2: Sitecore Item Security

Leverage Sitecore's native item security on Script Library items:

- Scripts inherit `item:read` permissions from their parent folder
- The remoting user's role must have `item:read` on the script item
- Combined with the existing `ServiceAuthorizationManager` role-based access

#### Layer 3: Profile Enforcement on Script Content

Even if a script is accessible via Layer 1 and Layer 2, the **active profile's restrictions still apply** to the script's content at execution time:

- Script is parsed via AST
- All commands validated against the profile's blocklist/allowlist
- If any blocked command is found, execution is denied with a 403

#### Workflow for Users

1. User creates a reporting script under their module's Script Library path
2. Admin adds the module path to `<remotingScripts>` include list (or uses Sitecore item-based config — see Section 6)
3. Script automatically inherits the active profile's restrictions
4. If the script needs capabilities beyond the active profile, the admin can either:
   a. Assign a less restrictive profile to that specific service endpoint
   b. Mark specific functions the script uses as "trusted" (Section 3)
   c. Create a dedicated service endpoint with appropriate restrictions

---

## 6. Hybrid Configuration Model

### Base Configuration (Spe.config)

Profiles are defined in `Spe.config` as XML patches. These ship with SPE and provide secure defaults:

```xml
<spe>
  <restrictionProfiles>
    <profile name="unrestricted" languageMode="FullLanguage">
      <commandRestrictions mode="none" />
    </profile>

    <profile name="read-only-sitecore" languageMode="ConstrainedLanguage">
      <commandRestrictions mode="blocklist">
        <blockedCommands>
          <command>Set-Item</command>
          <command>Remove-Item</command>
          <command>New-Item</command>
          <command>Move-Item</command>
          <command>Copy-Item</command>
          <!-- ... full list from Section 2 ... -->
        </blockedCommands>
      </commandRestrictions>
    </profile>

    <profile name="read-only-full" extends="read-only-sitecore"
             languageMode="ConstrainedLanguage">
      <commandRestrictions mode="blocklist">
        <blockedCommands>
          <!-- inherits parent + adds: -->
          <command>Invoke-SqlCommand</command>
          <command>Add-Type</command>
          <command>Set-Content</command>
          <!-- ... -->
        </blockedCommands>
      </commandRestrictions>
    </profile>

    <profile name="content-editor" languageMode="ConstrainedLanguage">
      <commandRestrictions mode="allowlist">
        <allowedCommands>
          <command>Get-Item</command>
          <command>Set-Item</command>
          <command>Get-ChildItem</command>
          <command>Publish-Item</command>
          <!-- ... full list from Section 2 ... -->
        </allowedCommands>
      </commandRestrictions>
    </profile>
  </restrictionProfiles>

  <!-- Service-to-profile mapping -->
  <services>
    <remoting profile="unrestricted" />   <!-- default unchanged -->
    <restfulv2 profile="unrestricted" />  <!-- default unchanged -->
  </services>
</spe>
```

### Item-Based Overrides (Sitecore Content Tree)

Administrators can extend/override profiles via Sitecore items under:
`/sitecore/system/Modules/PowerShell/Settings/Restriction Profiles/`

#### New Item Template: `PowerShell/Restriction Profile Override`

Fields:

- **Base Profile** (Droplink) — which config profile to extend
- **Additional Blocked Commands** (Multiline Text) — one command per line
- **Additional Allowed Commands** (Multiline Text) — one command per line
- **Trusted Function Paths** (Treelist) — Script Library paths to trust
- **Remoting Script Paths** (Treelist) — Script Library paths to expose
- **Audit Level** (Droplist) — None / Violations Only / Full

#### Merge Behavior

1. Config-based profile loads first (base)
2. Item-based overrides merge on top:
   - Additional blocked commands are **added** to the blocklist
   - Additional allowed commands are **added** to the allowlist
   - If base profile uses blocklist mode, item can only add more blocks (not remove)
   - If base profile uses allowlist mode, item can add more allows
3. Most restrictive wins in case of conflict

#### Benefits

- Admins can manage restrictions without config file deployments
- Changes take effect immediately (with cache invalidation)
- Auditable via Sitecore item versioning and workflow
- Role-based access to the Settings items themselves

---

## 7. Audit Logging

### Recommendation: Configurable Verbosity Per Profile

Each profile should have a configurable audit level:

| Level        | What's Logged                                             |
| ------------ | --------------------------------------------------------- |
| `None`       | No logging (current default behavior)                     |
| `Violations` | Blocked commands, denied .NET types, failed trust checks  |
| `Standard`   | Violations + script execution start/end with user context |
| `Full`       | Standard + every command executed with arguments          |

#### Configuration

```xml
<profile name="read-only-sitecore" languageMode="ConstrainedLanguage"
         auditLevel="Violations">
  <!-- ... -->
</profile>
```

Override via item-based config (see Section 6).

#### Log Format

```
SPE.Security [VIOLATION] User=sitecore\api-user Service=remoting Profile=read-only-sitecore
  BlockedCommand=Remove-Item Script=/sitecore/system/Modules/PowerShell/Script Library/Custom/Cleanup.ps1

SPE.Security [EXECUTION] User=sitecore\api-user Service=remoting Profile=read-only-sitecore
  Script=/sitecore/system/Modules/PowerShell/Script Library/Custom/Report.ps1 Duration=1.2s Status=Success
```

Logs write to Sitecore's standard log infrastructure (`Sitecore.Diagnostics.Log`).

---

## 8. Migration & Backward Compatibility

### Principle: Fully Backward-Compatible

All new profile functionality is **opt-in**. Existing installations see no behavior change.

### Migration Path

1. **Phase 1 (this PR):** Ship predefined profiles in `Spe.config` with all services defaulting to `unrestricted`
2. **Phase 2:** Add item-based override support and function trust registry
3. **Phase 3:** Add AST-based function analysis and SQL statement validation
4. **Phase 4:** Documentation and migration guide for adopters

### How to Adopt

Administrators opt-in by changing one attribute per service:

```xml
<!-- Before (implicit unrestricted) -->
<remoting enabled="true" requireSecureConnection="true" />

<!-- After (explicit profile) -->
<remoting enabled="true" requireSecureConnection="true" profile="read-only-sitecore" />
```

### Pipeline Scripts

Pipeline scripts (LoggedIn, LoggingIn, Logout hooks) **always run in FullLanguage** regardless of remoting profile settings. These are server-side only, admin-authored, and often require elevated access (Membership API, WebUtil.Redirect, etc.).

### Scope Claim Integration

The existing JWT `scope` claim should map to profile names:

- JWT with `scope=read-only-sitecore` → applies the `read-only-sitecore` profile
- JWT with `scope=content-editor` → applies the `content-editor` profile
- JWT with no scope or unknown scope → falls back to the service's default profile
- Multiple scopes → most restrictive profile wins

This builds on the existing `scopeRestrictions` system but formalizes it with named profiles.

---

## 9. Implementation Considerations

### Existing Infrastructure to Leverage

The codebase already has most building blocks:

| Component                   | File                                    | What It Does                                |
| --------------------------- | --------------------------------------- | ------------------------------------------- |
| Language mode enforcement   | `WebServiceSettings.cs:60-71`           | Per-service `PSLanguageMode` parsing        |
| Command blocklist/allowlist | `WebServiceSettings.cs`                 | Service-level restriction parsing           |
| AST-based script validation | `ScriptValidator.cs`                    | Parses scripts, extracts `CommandAst` nodes |
| Scope-based restrictions    | `ScriptValidator.cs`                    | JWT scope claim → restriction mapping       |
| Session elevation           | `SessionElevationManager.cs`            | UAC-style token system                      |
| Authorization caching       | `AuthCacheEntry.cs`                     | 10-second TTL cache for auth decisions      |
| JWT authentication          | `SharedSecretAuthenticationProvider.cs` | Scope and session claims                    |

### New Components Needed

1. **RestrictionProfile class** — Encapsulates a named profile (language mode + command restrictions + trust config + audit level)
2. **ProfileManager** — Loads profiles from config + items, handles merging, caching
3. **FunctionTrustRegistry** — Maps Script Library paths to trust levels
4. **AstAnalyzer** — Extended `ScriptValidator` that classifies functions as ReadOnly/WriteCapable/Elevated
5. **SqlStatementValidator** — Parses SQL for statement type validation
6. **Restriction Profile Override template** — New Sitecore item template

### Performance

- Profile resolution should be cached per service+scope combination
- AST analysis should be cached per script revision
- Trust registry lookups should use prefix-matching on Sitecore paths
- All caches should respect existing `Spe.AuthorizationCacheExpirationSecs` and `Spe.WebApiCacheExpirationSecs` settings

### Open Questions

1. Should profile inheritance support multiple levels (e.g., `read-only-full` extends `read-only-sitecore` extends base)?
2. Should there be a way to temporarily elevate a remoting session (like session elevation for CM users)?
3. How should `Import-Function` interact with function trust when the imported function itself calls `Import-Function`?
4. Should the AST analyzer run synchronously (blocking request) or asynchronously (pre-analyzed on script save)?
5. Should composable scopes be supported (JWT claims multiple scopes that intersect)?
