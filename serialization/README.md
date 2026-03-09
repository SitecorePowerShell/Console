# Sitecore Content Serialization (SCS) for SPE

SPE uses Sitecore Content Serialization (SCS) via the Sitecore CLI for item serialization.

## Prerequisites

- .NET SDK (version matching `serialization/.config/dotnet-tools.json`)
- A running Sitecore instance (10.1+) with SPE installed

## Initial Setup

Set the working directory to the `serialization/` folder before running any commands.

```bash
# Install the Sitecore CLI (pinned version from dotnet-tools.json)
dotnet tool restore

# Restore plugins for Sitecore CLI
dotnet sitecore plugin list

# Authenticate against your Sitecore instance
dotnet sitecore login --authority https://<identity-server> --cm https://<cm-host> --allow-write true
```

## Daily Workflow

| Task | Command |
|---|---|
| **Pull** items from Sitecore to disk | `dotnet sitecore ser pull` |
| **Push** items from disk to Sitecore | `dotnet sitecore ser push` |
| **Validate** serialized state matches Sitecore | `dotnet sitecore ser validate` |
| **Show** module/item info | `dotnet sitecore ser info` |
| **Generate** `.dat` resource files for packaging | `dotnet sitecore itemres create -o _out/spe --overwrite -i Spe.*` |

## Directory Layout

```
serialization/
  sitecore.json                    # Plugin config + defaultModuleRelativeSerializationPath
  modules/
    Spe.Core.module.json           # Module definitions (source of truth for item paths)
    Spe.Rules.module.json
    Spe.Scripts.module.json
    Spe.UI.module.json
    Spe.Roles.module.json
    Spe.Users.module.json
    serialization/                 # All serialized YAML lives here
      Spe.Core/
      Spe.Rules/
      Spe.Scripts/
      Spe.UI/
      Spe.Roles/
      Spe.Users/
```

## Key Notes

- **Module files are the source of truth** for item paths and scopes (`serialization/modules/*.module.json`).
- **No transparent sync** — SCS does not auto-sync changes. You must explicitly `ser pull` / `ser push`.
- **YAML format** — SCS YAML is not compatible with Rainbow (Unicorn) YAML. Do not mix them.
