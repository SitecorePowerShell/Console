# Local Deployment Configuration

SPE uses two JSON files to control how build output is deployed to local Sitecore instances. `deploy.json` is shared (checked in), `deploy.user.json` is per-developer (gitignored).

## Quick Start

1. Copy `deploy.user.json.sample` to `deploy.user.json`
2. Add your Sitecore site path(s) to the `sites` array
3. Run `task local:deploy` from the repository root

```json
{
    "sites": [
        {
            "path": "C:\\inetpub\\wwwroot\\my-sc.dev.local",
            "version": 10,
            "junction": true
        }
    ],
    "files": {
        "enable": [
            "App_Config/Include/Spe/Spe.IdentityServer.config"
        ]
    }
}
```

## deploy.json (shared)

This file defines the project structure and deployment rules. It is checked into the repository and should not contain machine-specific paths.

### Structure

```json
{
    "deployFolder": "_Deploy",
    "userConfigurationFolder": "UserConfiguration",
    "deployProjects": [ ... ],
    "sitesDefault": { ... }
}
```

### Fields

| Field | Description |
|---|---|
| `deployFolder` | Subdirectory under `src/` where MSBuild stages build output (default: `_Deploy`) |
| `userConfigurationFolder` | Subdirectory under `src/` containing user config templates with `%%variable%%` placeholders |
| `deployProjects` | Array of projects to deploy (see below) |
| `sitesDefault` | Default property values applied to every site unless overridden in `deploy.user.json` |

### deployProjects

Each entry describes a project whose build output should be deployed:

```json
{
    "project": "Spe",
    "minVersion": 0,
    "maxVersion": 10,
    "junctionPoints": [
        "sitecore modules\\PowerShell",
        "sitecore modules\\Shell\\PowerShell",
        "App_Config\\Include\\Spe",
        "App_Config\\Include\\z.Spe"
    ]
}
```

| Field | Required | Description |
|---|---|---|
| `project` | Yes | Project folder name under `src/`. Build output is read from `src/_Deploy/{project}/` |
| `minVersion` | Yes | Minimum Sitecore version for this project (matched against site `version`) |
| `maxVersion` | No | Maximum Sitecore version (inclusive). Omit for no upper bound |
| `junctionPoints` | No | Array of relative paths within the project to create as directory junctions instead of copying files. Only active when the site has `"junction": true`. Use double backslashes in JSON (`\\`) |

### sitesDefault

Default values merged into every site definition. Site-level properties override these.

```json
{
    "version": 8,
    "junction": false
}
```

## deploy.user.json (per-developer)

This file is gitignored. It defines your local Sitecore site paths, config file actions, and optional UI test credentials.

Copy `deploy.user.json.sample` as a starting point.

### Structure

```json
{
    "sites": [ ... ],
    "files": { ... }
}
```

### sites

Array of Sitecore instances to deploy to. Each site can override any property from `sitesDefault`.

```json
{
    "path": "C:\\inetpub\\wwwroot\\my-sc.dev.local",
    "version": 10,
    "junction": true,
    "test": {
        "url": "https://my-sc.dev.local",
        "username": "admin",
        "password": "b"
    }
}
```

| Field | Required | Description |
|---|---|---|
| `path` | Yes | Absolute path to the Sitecore webroot |
| `version` | No | Sitecore major version number (used to filter projects by `minVersion`/`maxVersion`). Defaults to `sitesDefault.version` |
| `junction` | No | When `true`, creates directory junctions for paths listed in the project's `junctionPoints` instead of copying files. Defaults to `sitesDefault.junction` |
| `test` | No | UI test credentials for Playwright tests. Only sites with this block are tested by `task ui:test` |
| `test.url` | Yes (if test) | Sitecore CM URL including scheme (e.g. `https://my-sc.dev.local`) |
| `test.username` | Yes (if test) | Sitecore login username |
| `test.password` | Yes (if test) | Sitecore login password |

### files

Config file actions applied to every site after file deployment.

```json
{
    "remove": [
        "App_Config/Include/Unicorn/Unicorn.DataProvider.config"
    ],
    "enable": [
        "App_Config/Include/Spe/Spe.IdentityServer.config"
    ],
    "disable": [
        "App_Config/Include/SomeFeature/Feature.config"
    ]
}
```

| Action | Description |
|---|---|
| `remove` | Delete the file from the site |
| `enable` | Rename `.disabled` or `.example` suffix to activate the config |
| `disable` | Rename the file by adding a `.disabled` suffix |

All paths are relative to the site's webroot.

## Junction Points

When a site has `"junction": true`, the deploy script creates Windows directory junctions from the site folder to the source project folder for each path listed in `junctionPoints`. This means:

- **Files in junctioned folders are live-editable.** Editing a JS, CSS, XML, or config file in the source tree is immediately visible on the site without redeploying.
- **Files in junctioned folders are not copied.** The deploy script skips them, reducing deploy time.
- **DLLs are never junctioned.** The `bin/` folder is always flat-copied because the build output goes to `_Deploy`, not the project source.

Choose junction points carefully. Only junction folders that are exclusively owned by SPE. Junctioning a shared Sitecore folder (like `App_Config/` or `sitecore/`) will destroy other modules' files.

Safe junction targets for the `Spe` project:

| Path | Contents |
|---|---|
| `sitecore modules\PowerShell` | JS, CSS, images, web services, PowerShell assets |
| `sitecore modules\Shell\PowerShell` | Sheer UI XML layouts (ISE, Console, dialogs) |
| `App_Config\Include\Spe` | SPE configuration files |
| `App_Config\Include\z.Spe` | SPE override configuration |

**Do not junction** `sitecore modules\` (contains other modules like SXA), `sitecore\` (contains login, shell, admin pages from other modules), or `App_Config\` (contains all Sitecore system configs).

## Taskfile Commands

| Command | Description |
|---|---|
| `task local:build` | Build the solution (finds MSBuild via vswhere, restores NuGet) |
| `task local:deploy` | Build and deploy to all sites in `deploy.user.json` |
| `task ui:test` | Run Playwright UI tests against sites with `test` configured |
| `task ui:test:headed` | Same as above but with a visible browser |

## Docker Deployment

The Docker-based `task deploy` uses a different code path. When no `deploy.user.json` exists, `Post_Build.ps1` defaults to deploying to `docker/deploy/`. The Docker tasks (`task up`, `task down`, `task deploy`) are independent from the local IIS tasks.
