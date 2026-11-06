# UI Test Conventions

## Content isolation

- All tests use `master:\content\spe-test` as their root item. Create it at the start, remove it at the end.
- Never read, list, or depend on items outside `spe-test`. Existing site content varies between developer machines.
- Use `Common/Folder` as the template for test items.
- If a test needs specific items, create them explicitly - do not assume they exist from a previous test run.
- Each test must clean up after itself even on failure. Structure scripts so cleanup runs last regardless of earlier errors.

## Script files

- Test PowerShell scripts live as `.ps1` files next to their `.spec.ts` files, named to match (e.g., `ise-writehost.ps1` for assertions in `ise.spec.ts`).
- Never inline PowerShell code in the TypeScript test files. The `.spec.ts` file loads the script from the `.ps1` file via `fs.readFileSync` and passes it to the browser.
- Scripts must be runnable manually in the ISE or Console for verification - do not put logic that only works via Playwright's `evaluate()`.

## Authentication

- The warmup test (`00-warmup.spec.ts`) logs in once and saves auth state to `.auth/`. All other tests reuse that state via `storageState`.
- Tests do not log in individually. If a test needs a fresh session for a specific reason, document why.
- Each test file calls `refreshSitecoreSession` in `afterAll`. It writes the renewed (sliding, 30 min) auth cookie back to `.auth/`, or logs in again if the session expired. Do not log out per file: every spec shares the warmup session.
- `99-logout.spec.ts` (the setup project's teardown) logs out once at the end of the run to free the license seat. A `--no-deps` run after a full run finds the session logged out, so include `00-warmup` in ad-hoc runs.
- Credentials come from `src/deploy.user.json` (gitignored) under each site's `test` block. Never hardcode credentials.

## Assertions

- Validate terminal output via `.terminal-output` locator's `innerText()`.
- Use `toContainText` (Playwright locator assertion) for content presence checks and `not.toMatch` for artifact absence checks.
- Always check for jsterm format artifacts: `expect(output).not.toMatch(/\[\[;/)` - if raw `[[;` appears in visible output, the terminal rendering is broken.
- Use `\s+` in regex when matching sequences that may wrap across visual lines (e.g., `expect(output).toMatch(/1\s+2\s+3/)`).

## Timeouts

- Script execution timeout: 90 seconds (some scripts create items and run cmdlets that can be slow on cold Sitecore).
- Element selectors: 15 seconds for UI elements, 60 seconds for terminal initialization (Console runs an init command on load).
- Login: 30 seconds (Identity Server redirect can be slow after app pool recycle).
