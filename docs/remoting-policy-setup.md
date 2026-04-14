<!-- TODO: Add this content to the SPE Book under the Remoting section. -->

# Setting up SPE Remoting for a Developer

You have a Sitecore instance with PowerShell Extensions installed. A developer needs to run scripts against it remotely — either sending script code directly or calling pre-built scripts stored in the Script Library.

## The pieces

**API Key** — Think of this as credentials for the developer. It contains a shared secret (like a password) that the developer uses to authenticate. You create one per consumer.

**Remoting Policy** — Think of this as a permission set. It controls what the developer is allowed to do. An API Key does nothing without one — the developer can log in but can't run anything until you attach a policy.

**The policy has three parts:**

1. **Full Language** (checkbox) — When unchecked, scripts run in a restricted mode that blocks access to .NET classes and advanced language features. When checked, scripts can do anything PowerShell can do. Leave it unchecked unless the developer specifically needs it.

2. **Allowed Commands** (text field) — This controls what the developer can do when they send raw script code to the server. List the commands they need, one per line — things like `Get-Item`, `Get-ChildItem`, `ConvertTo-Json`. If they send a script containing a command not on this list, it gets rejected.

3. **Approved Scripts** (picker) — This controls which pre-built scripts in the Web API folder the developer can call by name. Only scripts you select here will work. If a script is in the Web API folder but you didn't select it, the developer gets a 403 error.

## Step by step

### 1. Create a policy

Go to `/sitecore/system/Modules/PowerShell/Settings/Access/Policies/`. Right-click, Insert, choose "Remoting Policy." Name it something descriptive like "ReadOnly-Reporting."

- Leave **Full Language** unchecked
- In **Allowed Commands**, add only what the developer needs:
  ```
  Get-Item
  Get-ChildItem
  Select-Object
  ConvertTo-Json
  ```
- In **Approved Scripts**, select any Web API scripts they should be able to call by name

### 2. Create an API Key

Go to `/sitecore/system/Modules/PowerShell/Settings/Access/API Keys/`. Right-click, Insert, choose "Remoting API Key."

- Check **Enabled**
- Set a **Shared Secret** — a long, random string. Give this to the developer securely
- Set **Policy** — pick the policy you just created. **This is required.** Without it, every request gets denied.

### 3. Give the developer their credentials

They need:
- Your Sitecore hostname (e.g., `https://spe.dev.local`)
- The shared secret you set on their API Key

They'll use these to authenticate when calling the remoting endpoints.

## What happens when the developer makes a request

If they **send raw script code** (inline script):
- Each command in the script is checked against the Allowed Commands list
- Not on the list? Rejected.

If they **call a Web API script by name** (e.g., `/Reports/Active-Users`):
- The script is checked against the Approved Scripts list
- Not selected? Rejected.

Either way, the Full Language setting controls how much power the script has when it runs.

## Common mistakes

- **API Key with no policy** — The developer authenticates fine but every request returns 403. Assign a policy.
- **Script not in Approved Scripts** — The developer calls a Web API script by name and gets 403. You need to select it in the policy's Approved Scripts field.
- **Command not in Allowed Commands** — The developer's inline script fails with 403 naming the blocked command. Add it to the list.
- **Full Language unchecked but script needs .NET** — The script errors out at runtime. Only check Full Language if you understand what the script does and trust it.
