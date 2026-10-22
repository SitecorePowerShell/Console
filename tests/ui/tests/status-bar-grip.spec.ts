import { test, expect, Page, Frame } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";
import { loadTestSites, authStatePath, sitecoreLogout, TestSite } from "./sitecore-login";

const sites = loadTestSites();
const modalScript = fs.readFileSync(path.join(__dirname, "status-bar-grip.ps1"), "utf-8");
// core:/sitecore/content/Documents and settings/All users/Start menu/Right/PowerShell Console
const CONSOLE_SHORTCUT_ID = "{1B72C2A5-EAF1-47CE-86FF-4E3C42F1DD84}";

async function waitForFrame(page: Page, re: RegExp, selector: string): Promise<Frame> {
  for (let i = 0; i < 120; i++) {
    const f = page.frames().find((x) => re.test(x.url()));
    if (f && (await f.$(selector).catch(() => null))) return f;
    await page.waitForTimeout(250);
  }
  throw new Error(`frame ${re} not found: ` + page.frames().map((f) => f.url()).join(" | "));
}

// Drags the grip by (dx, dy) with real mouse events, the way a user would.
async function dragGrip(frame: Frame, dx: number, dy: number) {
  const box = (await frame.locator("#StatusBarGrip").boundingBox())!;
  const mouse = frame.page().mouse;
  // Grab the outermost corner pixel: it is outside the drawn grip but still
  // part of its hit area, and it is where a modal dialog's own corner handle
  // would otherwise sit on top.
  const x = box.x + box.width - 0.5, y = box.y + box.height - 0.5;
  await mouse.move(x, y);
  await mouse.down();
  await mouse.move(x + dx / 2, y + dy / 2, { steps: 5 });
  await mouse.move(x + dx, y + dy, { steps: 5 });
  await mouse.up();
}

const statusBarRight = (f: Frame) => f.evaluate(() => document.getElementById("StatusBar")!.getBoundingClientRect().right);

for (const site of sites) {
  test.describe(`Status bar resize grip - ${site.url}`, () => {
    test.use({ storageState: authStatePath(site) });

    test.afterAll(async ({ browser }) => {
      const context = await browser.newContext({ ignoreHTTPSErrors: true, storageState: authStatePath(site) });
      const page = await context.newPage();
      await sitecoreLogout(page, site);
      await context.close();
    });

    test("standalone ISE has no grip", async ({ page }) => {
      await page.goto(`${site.url}/sitecore/shell/Applications/PowerShell/PowerShellIse`, { waitUntil: "domcontentloaded" });
      await page.waitForSelector(".ace_editor", { timeout: 15_000 });
      await expect(page.locator("#StatusBar")).toBeVisible();
      await expect(page.locator("#StatusBarGrip")).toHaveCount(0);
    });

    test("grip resizes the ISE desktop window", async ({ page }) => {
      await page.goto(`${site.url}/sitecore/shell/default.aspx`, { waitUntil: "domcontentloaded" });
      await page.waitForFunction(() => (window as any).scManager, null, { timeout: 30_000 });
      await page.evaluate(() => {
        const td = Array.from(document.querySelectorAll("td.scMenuItemCaption")).find((e) => e.textContent === "PowerShell ISE");
        (td!.closest("tr") as HTMLElement).click();
      });
      const ise = await waitForFrame(page, /PowerShellIse/i, ".ace_editor");
      await expect(ise.locator("#StatusBarGrip")).toBeVisible();
      const host = page.locator("iframe[src*='xmlcontrol=Application']").last();
      const before = (await host.boundingBox())!;
      const rightBefore = await statusBarRight(ise);

      await dragGrip(ise, 120, 80);
      await expect.poll(async () => (await host.boundingBox())!.width).toBeGreaterThan(before.width + 100);
      const after = (await host.boundingBox())!;
      expect(after.height).toBeGreaterThan(before.height + 60);
      expect(after.x).toBeCloseTo(before.x, 0);
      expect(after.y).toBeCloseTo(before.y, 0);
      // The ISE layout follows the new frame size.
      await expect.poll(() => statusBarRight(ise)).toBeGreaterThan(rightBefore + 100);

      // Window manager minimum size is honoured.
      await dragGrip(ise, -2000, -2000);
      await expect.poll(async () => (await host.boundingBox())!.width).toBeLessThan(after.width);
      const min = (await host.boundingBox())!;
      expect(min.width).toBeGreaterThanOrEqual(150);
      expect(min.height).toBeGreaterThanOrEqual(64);
    });

    test("standalone Console has no grip", async ({ page }) => {
      await page.goto(`${site.url}/sitecore/shell/Applications/PowerShell/PowerShell-Console`, { waitUntil: "domcontentloaded" });
      await page.waitForSelector("#terminal .cmd", { timeout: 60_000 });
      await expect(page.locator("#StatusBarGrip")).toHaveCount(0);
    });

    test("grip resizes the Console desktop window", async ({ page }) => {
      await page.goto(`${site.url}/sitecore/shell/default.aspx`, { waitUntil: "domcontentloaded" });
      await page.waitForFunction(() => (window as any).scManager, null, { timeout: 30_000 });
      // The Console start menu item sits in the menu's right column; run its shortcut directly.
      await page.evaluate((id) => (window as any).scForm.postRequest("", "", "", `RunShortcut("${id}")`), CONSOLE_SHORTCUT_ID);
      const consoleFrame = await waitForFrame(page, /PowerShell-?Console/i, "#terminal .cmd");
      await expect(consoleFrame.locator("#StatusBarGrip")).toBeVisible();
      const host = page.locator("iframe[src*='xmlcontrol=Application']").last();
      const before = (await host.boundingBox())!;
      const terminalRight = () => consoleFrame.evaluate(() => document.getElementById("terminal")!.getBoundingClientRect().right);
      const rightBefore = await terminalRight();

      await dragGrip(consoleFrame, 120, 80);
      await expect.poll(async () => (await host.boundingBox())!.width).toBeGreaterThan(before.width + 100);
      const after = (await host.boundingBox())!;
      expect(after.height).toBeGreaterThan(before.height + 60);
      expect(after.x).toBeCloseTo(before.x, 0);
      expect(after.y).toBeCloseTo(before.y, 0);
      // The terminal follows the new frame size.
      await expect.poll(terminalRight).toBeGreaterThan(rightBefore + 100);
    });

    // Standalone ISE page, or the ISE opened in the Sitecore Desktop, where the
    // modal dialog frame sits under a window that has scManager.
    for (const host of ["standalone", "desktop"] as const) test(`grip resizes a modal ListView dialog (${host} ISE)`, async ({ page }) => {
      let ise: Page | Frame = page;
      if (host === "standalone") {
        await page.goto(`${site.url}/sitecore/shell/Applications/PowerShell/PowerShellIse`, { waitUntil: "domcontentloaded" });
        await page.waitForSelector(".ace_editor", { timeout: 15_000 });
      } else {
        await page.goto(`${site.url}/sitecore/shell/default.aspx`, { waitUntil: "domcontentloaded" });
        await page.waitForFunction(() => (window as any).scManager, null, { timeout: 30_000 });
        await page.evaluate(() => {
          const td = Array.from(document.querySelectorAll("td.scMenuItemCaption")).find((e) => e.textContent === "PowerShell ISE");
          (td!.closest("tr") as HTMLElement).click();
        });
        ise = await waitForFrame(page, /PowerShellIse/i, ".ace_editor");
      }
      await ise.evaluate((id) => (window as any).scForm.postRequest("", "", "", `ise:setsessionid(id=${id})`), `status-bar-grip-${Date.now()}`);
      await ise.evaluate((s) => (window as any).ace.edit(document.querySelector(".ace_editor") as HTMLElement).setValue(s, -1), modalScript);
      await ise.locator(".ace_editor").first().click();
      await page.keyboard.press("Control+e");

      const lv = await waitForFrame(page, /PowerShell(ListView|ResultViewerList)/i, "#StatusBar");
      await expect(lv.locator("#StatusBarGrip")).toBeVisible({ timeout: 15_000 });
      const widget = page.frameLocator("#jqueryModalDialogsFrame").locator(".ui-dialog").last();
      // Sitecore keeps the dialog at opacity 0.001 until the frame's load handler
      // has finished adjusting it; only then is it shown and safe to measure.
      await expect(widget).toHaveCSS("opacity", "1", { timeout: 15_000 });
      const initial = (await widget.boundingBox())!;

      // The dialog opens close to the viewport height and jQuery UI keeps it
      // inside the document, so shrink it first to leave room to grow.
      await dragGrip(lv, -200, -150);
      const before = (await widget.boundingBox())!;
      expect(before.width).toBeLessThan(initial.width - 150);
      expect(before.height).toBeLessThan(initial.height - 100);
      const rightBefore = await statusBarRight(lv);

      await dragGrip(lv, 100, 60);
      const after = (await widget.boundingBox())!;
      expect(after.width).toBeGreaterThan(before.width + 80);
      expect(after.height).toBeGreaterThan(before.height + 40);
      expect(after.x).toBeCloseTo(before.x, 0);
      expect(after.y).toBeCloseTo(before.y, 0);
      await expect.poll(() => statusBarRight(lv)).toBeGreaterThan(rightBefore + 80);

      // jQuery UI minWidth/minHeight are honoured.
      await dragGrip(lv, -3000, -3000);
      const min = (await widget.boundingBox())!;
      const opts = await page.frame({ name: "jqueryModalDialogsFrame" })!.evaluate(() => {
        const $ = (window as any).jQuery;
        const d = $("iframe[id^='scContentIframeId']").last();
        return { minWidth: d.dialog("option", "minWidth"), minHeight: d.dialog("option", "minHeight") };
      });
      expect(min.width).toBeLessThan(after.width);
      expect(min.width).toBeGreaterThanOrEqual(opts.minWidth - 1);
      expect(min.height).toBeGreaterThanOrEqual(opts.minHeight - 1);

      await page.goto("about:blank").catch(() => {});
    });
  });
}
