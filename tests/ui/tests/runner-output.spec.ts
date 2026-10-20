import { test, expect, Page, Frame, BrowserContext } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";
import { loadTestSites, authStatePath, sitecoreLogout, TestSite } from "./sitecore-login";

// The Runner keeps its script output in an embedded jquery.terminal that is
// hidden until the user opens it. While it is closed the Runner must cost
// nothing extra: no streaming, no terminal assets. At the end of the run the
// toggle turns into the run's verdict, and results never open a popup.

const sites = loadTestSites();
const setupScript = fs.readFileSync(path.join(__dirname, "runner-output-setup.ps1"), "utf-8");
const teardownScript = fs.readFileSync(path.join(__dirname, "runner-output-teardown.ps1"), "utf-8");

const RUNNER = /xmlcontrol=PowerShellRunner/i;
const RESULT_VIEWER = /PowerShellResultViewerText/i;
const CHILD_DIALOG = /PowerShellMultiValuePrompt/i;

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

async function runIseScript(context: BrowserContext, site: TestSite, script: string, marker: string): Promise<string> {
  const page = await context.newPage();
  await openIse(page, site);
  await page.evaluate(() => (window as any).scForm.postRequest("", "", "", "ise:setsessionid(id=runner-output-setup)"));
  await runInIse(page, script);
  const terminal = page.locator(".terminal-output");
  await expect(terminal).toContainText(marker, { timeout: 90_000 });
  const text = await terminal.innerText();
  await page.close();
  return text;
}

async function openContentEditor(page: Page, site: TestSite, folderId: string) {
  await page.goto(
    `${site.url}/sitecore/shell/Applications/Content%20Editor.aspx?fo=${encodeURIComponent(folderId)}`,
    { waitUntil: "domcontentloaded" }
  );
  await page.waitForFunction(() => !!(window as any).scForm && !!(window as any).top._scDialogs, null, {
    timeout: 30_000,
  });
}

async function launch(page: Page, scriptId: string, itemId: string): Promise<Frame> {
  await page.evaluate(
    ([s, i]) =>
      (window as any).scForm.postRequest("", "", "", `item:executescript(id=${i},db=master,script=${s},scriptDb=master)`),
    [scriptId, itemId]
  );
  const frame = await waitForFrame(page, RUNNER);
  await frame.locator("#OutputToggle").waitFor({ state: "visible", timeout: 15_000 });
  return frame;
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
  throw new Error(`No frame matching ${pattern}`);
}

async function title(frame: Frame): Promise<string> {
  return (await frame.locator("#Title").innerText().catch(() => "")).trim();
}

async function expectDone(frame: Frame, timeout = 90_000) {
  await expect.poll(() => title(frame), { timeout, intervals: [500] }).toBe("Done!");
}

async function hostSize(frame: Frame): Promise<{ w: number; h: number }> {
  const box = await (await frame.frameElement()).boundingBox();
  return { w: Math.round(box!.width), h: Math.round(box!.height) };
}

function toggle(frame: Frame) {
  return frame.locator("#OutputToggle");
}

function normalize(s: string): string {
  return s.replace(/\u00A0/g, " ");
}

async function terminalText(frame: Frame): Promise<string> {
  return normalize(await frame.locator("#RunnerTerminal .terminal-output").innerText().catch(() => ""));
}

async function openOutput(frame: Frame) {
  await toggle(frame).click();
  await expect(toggle(frame)).toHaveAttribute("aria-expanded", "true");
  await frame.locator("#RunnerTerminal .terminal-output").waitFor({ state: "attached", timeout: 15_000 });
}

async function closeRunner(page: Page, frame: Frame) {
  await frame.locator("#OkButton").click();
  await expect.poll(() => page.frames().some((f) => RUNNER.test(f.url())), { timeout: 15_000 }).toBe(false);
}

function countOf(text: string, token: string): number {
  return text.split(/\s+/).filter((t) => t === token).length;
}

for (const site of sites) {
  test.describe(`Runner output - ${site.url}`, () => {
    test.describe.configure({ mode: "serial" });
    test.use({ storageState: authStatePath(site) });

    let ids: Ids = {};

    test.beforeAll(async ({ browser }) => {
      const context = await browser.newContext({ ignoreHTTPSErrors: true, storageState: authStatePath(site) });
      const text = await runIseScript(context, site, setupScript, "SETUP_OK");
      for (const m of text.matchAll(/(\w+)=(\{[0-9A-F-]{36}\})/gi)) ids[m[1]] = m[2];
      await context.close();
      expect(ids.FOLDER, "setup printed the folder id").toBeTruthy();
    });

    test.afterAll(async ({ browser }) => {
      const context = await browser.newContext({ ignoreHTTPSErrors: true, storageState: authStatePath(site) });
      await runIseScript(context, site, teardownScript, "TEARDOWN_OK");
      const page = await context.newPage();
      await sitecoreLogout(page, site);
      await context.close();
    });

    test.beforeEach(async ({ page }) => {
      await openContentEditor(page, site, ids.FOLDER);
    });

    test("starts collapsed and the toggle grows and restores the dialog", async ({ page }) => {
      const runner = await launch(page, ids.RoSleep, ids.FOLDER);
      await expect(toggle(runner)).toHaveAttribute("aria-expanded", "false");
      await expect(runner.locator("#RunnerOutput")).toBeHidden();
      const collapsed = await hostSize(runner);

      await openOutput(runner);
      await expect(runner.locator("#RunnerOutput")).toBeVisible();
      await expect.poll(async () => (await hostSize(runner)).w, { timeout: 5_000 }).toBeGreaterThanOrEqual(700);

      await toggle(runner).click();
      await expect(toggle(runner)).toHaveAttribute("aria-expanded", "false");
      await expect(runner.locator("#RunnerOutput")).toBeHidden();
      await expect
        .poll(async () => {
          const s = await hostSize(runner);
          return Math.abs(s.w - collapsed.w) <= 2 && Math.abs(s.h - collapsed.h) <= 2;
        }, { timeout: 5_000 })
        .toBe(true);

      await expectDone(runner);
      await closeRunner(page, runner);
    });

    test("the open terminal sits below the progress bar and its status lines", async ({ page }) => {
      const runner = await launch(page, ids.RoProgress, ids.FOLDER);
      await expect(runner.locator("#PsProgress")).toContainText("Step", { timeout: 15_000 });
      await openOutput(runner);
      await page.waitForTimeout(600);
      const layout = await runner.evaluate(() => {
        const bottom = (id: string) => (document.getElementById(id) as HTMLElement).getBoundingClientRect().bottom;
        return {
          bar: bottom("progressbar"),
          lines: bottom("PsProgress"),
          terminalTop: (document.getElementById("RunnerTerminal") as HTMLElement).getBoundingClientRect().top,
        };
      });
      expect(layout.terminalTop, "terminal clears the progress bar").toBeGreaterThanOrEqual(layout.bar + 8);
      expect(layout.terminalTop, "terminal clears the time remaining / operation lines").toBeGreaterThanOrEqual(layout.lines + 8);
      await runner.locator("#AbortButton").click();
      await expect(toggle(runner)).toHaveAttribute("data-status", "aborted", { timeout: 30_000 });
      await closeRunner(page, runner);
    });

    test("streams output while the script is still running", async ({ page }) => {
      const runner = await launch(page, ids.RoStream, ids.FOLDER);
      await openOutput(runner);
      await expect.poll(() => terminalText(runner), { timeout: 15_000 }).toContain("line-2");
      expect(await title(runner)).not.toBe("Done!");
      await expectDone(runner);
      await expect.poll(() => terminalText(runner)).toContain("stream-done");
      await closeRunner(page, runner);
    });

    test("opening mid-run replays the lines written before it was opened", async ({ page }) => {
      const runner = await launch(page, ids.RoEarlyLate, ids.FOLDER);
      await page.waitForTimeout(2_000);
      await openOutput(runner);
      await expect.poll(() => terminalText(runner), { timeout: 10_000 }).toContain("early");
      expect(await terminalText(runner)).not.toContain("late");
      await expect.poll(() => terminalText(runner), { timeout: 15_000 }).toContain("late");
      await expectDone(runner);
      await closeRunner(page, runner);
    });

    test("a clean run turns the toggle into Success and shows results in the terminal, not a popup", async ({ page }) => {
      const runner = await launch(page, ids.RoClean, ids.FOLDER);
      await expectDone(runner);
      await expect(toggle(runner)).toHaveAttribute("data-status", "ok");
      await expect(toggle(runner)).toContainText("Success");
      await expect(runner.locator("#ViewButton, #ViewErrorsButton")).toHaveCount(0);
      await openOutput(runner);
      await expect.poll(() => terminalText(runner)).toContain("hello");
      expect(await terminalText(runner)).not.toMatch(/\[\[;/);
      expect(page.frames().some((f) => RESULT_VIEWER.test(f.url()))).toBe(false);
      await closeRunner(page, runner);
    });

    test("warnings and errors turn the toggle into their counts", async ({ page }) => {
      const runner = await launch(page, ids.RoProblems, ids.FOLDER);
      await expectDone(runner);
      await expect(toggle(runner)).toHaveAttribute("data-status", "problems");
      await expect(toggle(runner)).toContainText("2 warnings");
      await expect(toggle(runner)).toContainText("1 error");
      await openOutput(runner);
      await expect.poll(() => terminalText(runner)).toContain("problems-done");
      const text = await terminalText(runner);
      expect(text).toContain("warn-a");
      expect(text).toContain("err-c");
      await closeRunner(page, runner);
    });

    test("an aborted run says Aborted", async ({ page }) => {
      const runner = await launch(page, ids.RoAbort, ids.FOLDER);
      await page.waitForTimeout(1_500);
      await runner.locator("#AbortButton").click();
      await expect(toggle(runner)).toHaveAttribute("data-status", "aborted", { timeout: 30_000 });
      await expect(toggle(runner)).toContainText("Aborted");
      await closeRunner(page, runner);
    });

    test("fast output arrives exactly once", async ({ page }) => {
      const runner = await launch(page, ids.RoFast, ids.FOLDER);
      await openOutput(runner);
      await expectDone(runner);
      await expect.poll(() => terminalText(runner)).toContain("fast-50");
      const text = await terminalText(runner);
      for (let i = 1; i <= 50; i++) expect(countOf(text, `fast-${i}`), `fast-${i}`).toBe(1);
      await closeRunner(page, runner);
    });

    test("output produced while closed costs nothing until the user opens it", async ({ page }) => {
      const bodies: string[] = [];
      const assetRequests: string[] = [];
      page.on("request", (r) => {
        if (/jquery\.terminal/i.test(r.url())) assetRequests.push(r.url());
      });
      page.on("response", async (r) => {
        if (r.request().method() === "POST" && RUNNER.test(r.request().frame()?.url() ?? "")) {
          bodies.push(await r.text().catch(() => ""));
        }
      });

      const runner = await launch(page, ids.RoQuiet, ids.FOLDER);
      await expectDone(runner);
      expect(bodies.length, "the Runner polled while running").toBeGreaterThan(0);
      expect(bodies.filter((b) => /appendOutput|updatePartialOutput/.test(b))).toHaveLength(0);
      expect(assetRequests).toHaveLength(0);

      await openOutput(runner);
      await expect.poll(() => terminalText(runner)).toContain("quiet-30");
      const text = await terminalText(runner);
      for (let i = 1; i <= 30; i++) expect(countOf(text, `quiet-${i}`), `quiet-${i}`).toBe(1);
      await closeRunner(page, runner);
    });

    test("Write-Host -NoNewline parts join into one line", async ({ page }) => {
      const runner = await launch(page, ids.RoPartial, ids.FOLDER);
      await openOutput(runner);
      await expectDone(runner);
      // jsterm renders each Write-Host segment with a trailing space, as in
      // the ISE; what matters is that "cd" arrived and stayed on the same line.
      await expect.poll(() => terminalText(runner)).toMatch(/ab ?cd/);
      // the streamed partial "ab" is replaced by the final line, not kept beside it
      expect(countOf(await terminalText(runner), "ab")).toBe(1);
      await closeRunner(page, runner);
    });

    test("output streamed while open and the burst at exit each arrive once", async ({ page }) => {
      const runner = await launch(page, ids.RoBurst, ids.FOLDER);
      await openOutput(runner);
      await expect.poll(() => terminalText(runner), { timeout: 15_000 }).toContain("s-3");
      await expectDone(runner);
      await expect.poll(() => terminalText(runner)).toContain("t-40");
      const text = await terminalText(runner);
      for (let i = 1; i <= 12; i++) expect(countOf(text, `s-${i}`), `s-${i}`).toBe(1);
      for (let i = 1; i <= 40; i++) expect(countOf(text, `t-${i}`), `t-${i}`).toBe(1);
      expect(countOf(text, "tail"), "trailing -NoNewline line").toBe(1);
      await closeRunner(page, runner);
    });

    test("each error record counts once, including multi-line messages", async ({ page }) => {
      const runner = await launch(page, ids.RoTwoErrors, ids.FOLDER);
      await expectDone(runner);
      await expect(toggle(runner)).toHaveAttribute("data-status", "problems");
      await expect(toggle(runner)).toContainText("2 errors");
      await closeRunner(page, runner);
    });

    test("a raw host error line does not swallow the next error", async ({ page }) => {
      const runner = await launch(page, ids.RoHostErr, ids.FOLDER);
      await expectDone(runner);
      await expect(toggle(runner)).toContainText("2 errors");
      await closeRunner(page, runner);
    });

    test("closing before the terminal assets arrive keeps the server from streaming", async ({ page }) => {
      // Hold the terminal script back so the first open is still loading when
      // the user closes again.
      await page.route(/jquery\.terminal\.js/i, async (route) => {
        await new Promise((r) => setTimeout(r, 2_500));
        await route.continue();
      });
      const bodies: string[] = [];
      page.on("response", async (r) => {
        if (r.request().method() === "POST" && RUNNER.test(r.request().frame()?.url() ?? "")) {
          bodies.push(await r.text().catch(() => ""));
        }
      });

      const runner = await launch(page, ids.RoAbort, ids.FOLDER);
      await toggle(runner).click();
      await toggle(runner).click();
      await expect(toggle(runner)).toHaveAttribute("aria-expanded", "false");
      await page.waitForTimeout(3_500);
      bodies.length = 0;
      await page.waitForTimeout(2_000);
      expect(bodies.length, "the Runner kept polling").toBeGreaterThan(0);
      expect(bodies.filter((b) => /appendOutput|updatePartialOutput/.test(b))).toHaveLength(0);
      await runner.locator("#AbortButton").click();
      await expect(toggle(runner)).toHaveAttribute("data-status", "aborted", { timeout: 30_000 });
      await closeRunner(page, runner);
    });

    test("the terminal uses the console background the ISE uses", async ({ page }) => {
      const runner = await launch(page, ids.RoClean, ids.FOLDER);
      await openOutput(runner);
      const bg = await runner.evaluate(() => getComputedStyle(document.getElementById("RunnerTerminal") as HTMLElement).backgroundColor);
      // default console background (DarkBlue) as the ISE/Console render it: #012456
      expect(bg).toBe("rgb(1, 36, 86)");
      await expectDone(runner);
      await closeRunner(page, runner);
    });

    test("Clear-Host leaves only what came after it", async ({ page }) => {
      const runner = await launch(page, ids.RoClear, ids.FOLDER);
      await openOutput(runner);
      await expectDone(runner);
      await expect.poll(() => terminalText(runner)).toContain("after");
      expect(await terminalText(runner)).not.toContain("before");
      await closeRunner(page, runner);
    });

    test("counts reset between runs of a persistent session", async ({ page }) => {
      let runner = await launch(page, ids.RoPersistErr, ids.FOLDER);
      await expectDone(runner);
      await expect(toggle(runner)).toHaveAttribute("data-status", "problems");
      await closeRunner(page, runner);

      runner = await launch(page, ids.RoPersistOk, ids.FOLDER);
      await expectDone(runner);
      await expect(toggle(runner)).toHaveAttribute("data-status", "ok");
      await closeRunner(page, runner);
    });

    test("a child dialog is never resized by the toggle", async ({ page }) => {
      const runner = await launch(page, ids.RoChild, ids.FOLDER);
      await openOutput(runner);
      const child = await waitForFrame(page, CHILD_DIALOG);
      const before = await hostSize(child);
      await runner.evaluate(() => (document.getElementById("OutputToggle") as HTMLElement).click());
      await page.waitForTimeout(800);
      const after = await hostSize(child);
      expect(after).toEqual(before);
      await child.locator("#OKButton").click();
      await expectDone(runner);
      await closeRunner(page, runner);
    });

    test("Close-Window still closes the Runner on its own", async ({ page }) => {
      await launch(page, ids.RoCloseWin, ids.FOLDER);
      await expect.poll(() => page.frames().some((f) => RUNNER.test(f.url())), { timeout: 60_000 }).toBe(false);
    });
  });
}
