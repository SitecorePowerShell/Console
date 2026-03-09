# SCWDP Template Files

These files are used by `scripts/convert-to-scwdp.ps1` to build Web Deploy Packages (`.scwdp.zip`) from Sitecore packages. They replace the external Sitecore Azure Toolkit (SAT) dependency.

## What's here

| File | Purpose |
|------|---------|
| `postdeploy.sql` | SQL script bundled into `core.dacpac`. Creates the SPE remoting role (`sitecore\PowerShell Extensions Remoting`) and API user (`sitecore\PowerShellExtensionsAPI`) in the Sitecore Core database. |
| `model.xml` | Empty database schema definition for the dacpac (SQL Server 2016 / compat level 130). |
| `DacMetadata.xml` | Dacpac version metadata. |
| `Origin.xml` | Dacpac package origin/provenance info. |
| `Content_Types.xml` | OPC content types manifest for the dacpac ZIP. Renamed from `[Content_Types].xml` to avoid filesystem issues with brackets; the build script restores the original name when creating the dacpac. |

## How the build works

1. `convert-to-scwdp.ps1` zips these files into `core.dacpac` (renaming `Content_Types.xml` back to `[Content_Types].xml`)
2. Generates MSDeploy metadata (`archive.xml`, `parameters.xml`, `SystemInfo.xml`)
3. Extracts `files/*` from the nested `package.zip` inside the Sitecore package into `Content/Website/`
4. Bundles everything into the final `.scwdp.zip`

## Updating for future versions

- **New roles/users**: Edit `postdeploy.sql` directly. It runs against the Sitecore Core database during Web Deploy.
- **Schema changes**: Update `model.xml` if the dacpac needs to target a different SQL Server version.
- **Other files** (`DacMetadata.xml`, `Origin.xml`, `Content_Types.xml`) are static and rarely need changes.

## Origin

These files were extracted from the `core.dacpac` produced by SAT's `ConvertTo-SCModuleWebDeployPackage` cmdlet (Sitecore Azure Toolkit 2.8.1). They are now maintained as editable source files.
