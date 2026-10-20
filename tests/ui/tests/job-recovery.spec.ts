import { test, expect, Page, Frame, BrowserContext } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";
import { loadTestSites, authStatePath, sitecoreLogout, TestSite } from "./sitecore-login";

// Interactive script jobs must survive the browser freezing or discarding the
// tab: SpeJobRecovery.js revives the poll, InteractiveJobRegistry keeps the
// job's queue past its AfterLife, and a reloaded page re-attaches to the job.

const sites = loadTestSites();
const setupScript = fs.readFileSync(path.join(__dirname, "job-recovery-setup.ps1"), "utf-8");
const teardownScript = fs.readFileSync(path.join(__dirname, "job-recovery-teardown.ps1"), "utf-8");
const iseDialogScript = fs.readFileSync(path.join(__dirname, "job-recovery-ise-dialog.ps1"), "utf-8");
const consoleScript = fs.readFileSync(path.join(__dirname, "job-recovery-console.ps1"), "utf-8").trim();

const RUNNER = /xmlcontrol=PowerShellRunner/i;
const DIALOG = /PowerShellMultiValuePrompt/i;
const LISTVIEW = /PowerShell(ListView|ResultViewerList)/i;

type Ids = Record<string, string>;

async function openIse(page: Page, site: TestSite) {
  await page.goto(`${site.url}/sitecore/shell/Applications/PowerShell/PowerShellIse`, { waitUntil: "domcontentloaded" });
  await page.waitForSelector(".ace_editor", { timeout: 15_000 });
}

async function runInIse(page: Page, script: string) {
  await page.evaluate((s) => {
    const editor = (window as any).ace.edit(document.querySelector(".ace_editor") as HTMLElement);
    editor.setValue(s, -1);
  }, script);
  await page.locator(".ace_editor").first().click();
  await page.keyboard.press("Control+e");
}

async function setup(context: BrowserContext, site: TestSite): Promise<Ids> {
  const page = await context.newPage();
  await openIse(page, site);
  await page.evaluate(() => (window as any).scForm.postRequest("", "", "", "ise:setsessionid(id=job-recovery-setup)"));
  await runInIse(page, setupScript);
  const terminal = page.locator(".terminal-output");
  await expect(terminal).toContainText("SETUP_OK", { timeout: 90_000 });
  const text = await terminal.innerText();
  const ids: Ids = {};
  for (const m of text.matchAll(/(\w+)=(\{[0-9A-F-]{36}\})/gi)) ids[m[1]] = m[2];
  await page.close();
  return ids;
}

async function openContentEditor(page: Page, site: TestSite, folderId: string) {
  await page.goto(
    `${site.url}/sitecore/shell/Applications/Content%20Editor.aspx?fo=${encodeURIComponent(folderId)}`,
    { waitUntil: "domcontentloaded" }
  );
  await waitForShellReady(page);
}

// scForm.showModalDialog needs top._scDialogs, set up once the page loaded.
async function waitForShellReady(page: Page) {
  await page.waitForFunction(
    () => !!(window as any).scForm && !!(window as any).speJobWatchdog && !!(window as any).top._scDialogs,
    null,
    { timeout: 30_000 }
  );
}

async function launch(page: Page, scriptId: string, itemId: string) {
  await page.evaluate(
    ([s, i]) =>
      (window as any).scForm.postRequest("", "", "", `item:executescript(id=${i},db=master,script=${s},scriptDb=master)`),
    [scriptId, itemId]
  );
}

async function waitForFrame(page: Page, pattern: RegExp, timeout = 60_000): Promise<Frame> {
  const end = Date.now() + timeout;
  while (Date.now() < end) {
    const frame = page.frames().find((f) => pattern.test(f.url()));
    if (frame) {
      try {
        await frame.waitForLoadState("domcontentloaded", { timeout: 5_000 });
        return frame;
      } catch {
        // frame navigated away while we waited; look again
      }
    }
    await page.waitForTimeout(250);
  }
  throw new Error(`No frame matching ${pattern} in: ${page.frames().map((f) => f.url()).join(" | ")}`);
}

async function runnerTitle(page: Page): Promise<string> {
  const frame = page.frames().find((f) => RUNNER.test(f.url()));
  if (!frame) return "";
  return (await frame.locator("#Title").innerText().catch(() => "")).trim();
}

async function expectRunnerDone(page: Page, timeout = 90_000) {
  await expect.poll(() => runnerTitle(page), { timeout, intervals: [500] }).toBe("Done!");
}

async function closeTopDialog(page: Page) {
  const host = await waitForFrame(page, /JqueryModalDialogs\.html/i);
  await host.evaluate(() => (window as any).dialogClose());
}

// Emulates a frozen tab. CDP Page.setWebLifecycleState is ignored for a
// visible page (timers and requests carry on), so pause the page's JavaScript
// in the debugger instead: no timers, no polls, no requests until resumed.
async function freezeStart(page: Page) {
  const cdp = await page.context().newCDPSession(page);
  await cdp.send("Debugger.enable");
  await cdp.send("Debugger.pause");
  return async () => {
    await cdp.send("Debugger.resume");
    await cdp.send("Debugger.disable");
    await cdp.detach();
  };
}

async function freeze(page: Page, ms: number) {
  const unfreeze = await freezeStart(page);
  await new Promise((r) => setTimeout(r, ms));
  await unfreeze();
}

for (const site of sites) {
  test.describe(`Job recovery - ${site.url}`, () => {
    test.describe.configure({ mode: "serial" });
    test.use({ storageState: authStatePath(site) });

    let ids: Ids = {};

    test.beforeAll(async ({ browser }) => {
      const context = await browser.newContext({ ignoreHTTPSErrors: true, storageState: authStatePath(site) });
      ids = await setup(context, site);
      await context.close();
      expect(ids.FOLDER, "setup printed the folder id").toBeTruthy();
    });

    test.afterAll(async ({ browser }) => {
      const context = await browser.newContext({ ignoreHTTPSErrors: true, storageState: authStatePath(site) });
      const page = await context.newPage();
      await openIse(page, site);
      await runInIse(page, teardownScript);
      await expect(page.locator(".terminal-output")).toContainText("TEARDOWN_OK", { timeout: 60_000 });
      await sitecoreLogout(page, site);
      await context.close();
    });

    test("recovery script is on Sheer pages and posts the tab id", async ({ page }) => {
      await openContentEditor(page, site, ids.FOLDER);
      const state = await page.evaluate(() => ({
        tabId: (window as any).speJobWatchdog.tabId,
        stored: window.sessionStorage.getItem("spe.tabId"),
        field: (document.getElementById("__SPETABID") as HTMLInputElement | null)?.value,
      }));
      expect(state.tabId).toMatch(/^[0-9a-f]{32}$/);
      expect(state.stored).toBe(state.tabId);
      expect(state.field).toBe(state.tabId);
    });

    test("reloaded Content Editor re-opens the Runner with the pending dialog", async ({ page }) => {
      test.setTimeout(180_000);
      await openContentEditor(page, site, ids.FOLDER);
      await launch(page, ids.RecoveryDialog, ids.FOLDER);
      await waitForFrame(page, DIALOG);

      // Simulates a discarded tab: same tab (sessionStorage survives), new page.
      await page.reload({ waitUntil: "domcontentloaded" });
      await waitForShellReady(page);

      const dialog = await waitForFrame(page, DIALOG, 60_000);
      await dialog.locator("#OKButton").click();
      await expectRunnerDone(page);
    });

    test("frozen tab still gets the ListView the script queued after its job was evicted", async ({ page }) => {
      test.setTimeout(180_000);
      await openContentEditor(page, site, ids.FOLDER);
      await launch(page, ids.RecoveryListView, ids.FOLDER);
      await waitForFrame(page, RUNNER);

      // The script runs 15s then queues Show-ListView; 35s frozen leaves the
      // finished job well past its 10s AfterLife before the page polls again.
      await freeze(page, 35_000);

      await waitForFrame(page, LISTVIEW, 60_000);
      // Sheer holds the rest of that response (the Runner's "Done!") until
      // the modal closes, as it always has; close the ListView to see it.
      await closeTopDialog(page);
      // Progress messages drained in the same catch-up poll must not repaint
      // the title over "Done!".
      await expectRunnerDone(page);
    });

    test("a failed poll does not strand the Runner", async ({ page }) => {
      test.setTimeout(180_000);
      await openContentEditor(page, site, ids.FOLDER);
      let failed = 0;
      await page.route(/default\.aspx\?xmlcontrol=PowerShellRunner/i, async (route) => {
        const body = route.request().postData() ?? "";
        if (failed === 0 && /pstaskmonitor%3Acheck|pstaskmonitor:check/i.test(body)) {
          failed++;
          await route.fulfill({ status: 500, contentType: "text/html", body: "<html><body>boom</body></html>" });
          return;
        }
        await route.continue();
      });
      page.on("dialog", (d) => d.dismiss().catch(() => {}));

      await launch(page, ids.RecoveryShort, ids.FOLDER);
      await expectRunnerDone(page, 90_000);
      expect(failed).toBe(1);
    });

    test("ISE reloaded while a dialog is open gets the dialog back", async ({ page }) => {
      test.setTimeout(180_000);
      await openIse(page, site);
      await page.evaluate(() => (window as any).scForm.postRequest("", "", "", "ise:setsessionid(id=job-recovery-ise)"));
      await runInIse(page, iseDialogScript);
      await waitForFrame(page, DIALOG);

      await page.reload({ waitUntil: "domcontentloaded" });
      await page.waitForSelector(".ace_editor", { timeout: 30_000 });

      const dialog = await waitForFrame(page, DIALOG, 60_000);
      await dialog.locator("#OKButton").click();
      await expect(page.locator(".terminal-output")).toContainText("ISE_RESULT=ok ISE_ANSWER=ise-default", {
        timeout: 60_000,
      });
    });

    test("Console picks up a command's output after the tab was frozen", async ({ page }) => {
      test.setTimeout(180_000);
      await page.goto(`${site.url}/sitecore/shell/applications/powershell/powershell-console`, {
        waitUntil: "domcontentloaded",
      });
      await page.waitForFunction(
        () => {
          const jq = (window as any).jQuery;
          const term = jq && jq("#terminal").length ? jq("#terminal").terminal() : null;
          return !!term && !term.paused();
        },
        null,
        { timeout: 60_000 }
      );
      await page.evaluate((s) => (window as any).jQuery("#terminal").terminal().exec(s), consoleScript);
      await page.waitForTimeout(2_000);

      // 25s command, 40s frozen: the job is past its 20s AfterLife on wake.
      await freeze(page, 40_000);

      const terminal = page.locator(".terminal-output");
      await expect(terminal).toContainText("CONSOLE_RECOVERY_DONE", { timeout: 60_000 });
      const output = await terminal.innerText();
      expect(output).not.toContain("Communication error");
      expect(output).not.toContain("Can't find your command result");
    });

    test("only one tab renders a job; an abandoned job moves to the tab in use", async ({ context, page }) => {
      test.setTimeout(420_000);
      const tabA = page;
      await openContentEditor(tabA, site, ids.FOLDER);
      await launch(tabA, ids.RecoveryLong, ids.FOLDER);
      await waitForFrame(tabA, RUNNER);

      // A second tab of the same user loads while A is alive: A keeps the job.
      const tabB = await context.newPage();
      await openContentEditor(tabB, site, ids.FOLDER);
      await tabB.waitForTimeout(8_000);
      expect(tabB.frames().some((f) => RUNNER.test(f.url()))).toBe(false);

      // A goes quiet past ClaimAbandonedSeconds (120s); B reloads and claims.
      const unfreezeA = await freezeStart(tabA);
      await tabB.waitForTimeout(130_000);
      await tabB.reload({ waitUntil: "domcontentloaded" });
      await waitForShellReady(tabB);
      await waitForFrame(tabB, RUNNER, 30_000);

      // A wakes, learns on its next poll that it lost the lease, and stops
      // rendering the job.
      await unfreezeA();
      await expect
        .poll(() => runnerTitle(tabA), { timeout: 40_000, intervals: [500] })
        .toBe("This script is now shown in another window.");

      // The re-attached Runner replays the output written before it took
      // over once its output terminal is opened.
      const runnerB = tabB.frames().find((f) => RUNNER.test(f.url()))!;
      await runnerB.locator("#OutputToggle").click();
      await expect(runnerB.locator("#RunnerTerminal .terminal-output")).toContainText("RECOVERY_LONG_STARTED", {
        timeout: 15_000,
      });

      await expectRunnerDone(tabB, 120_000);
      await expect(runnerB.locator("#RunnerTerminal .terminal-output")).toContainText("RECOVERY_LONG_DONE");
      await tabB.close();
    });
  });
}
