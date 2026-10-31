import { test, expect, Page, Frame } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";
import { loadTestSites, authStatePath, sitecoreLogout, TestSite } from "./sitecore-login";

const sites = loadTestSites();
const script = fs.readFileSync(path.join(__dirname, "read-variable-image.ps1"), "utf-8");
const teardownScript = fs.readFileSync(path.join(__dirname, "read-variable-image-teardown.ps1"), "utf-8");

const DIALOG = /PowerShellMultiValuePrompt/i;
// the modal host URL embeds the dialog URL, so match the inner page only
const MEDIA_BROWSER = /^(?!.*JqueryModalDialogs).*MediaBrowser/i;

async function openIse(page: Page, site: TestSite) {
  await page.goto(`${site.url}/sitecore/shell/Applications/PowerShell/PowerShellIse`, { waitUntil: "domcontentloaded" });
  await page.waitForSelector(".ace_editor", { timeout: 15_000 });
  await closeOrphanedDialogs(page);
}

// A dialog left open by an earlier failed run is re-attached to the ISE on load; cancel it.
async function closeOrphanedDialogs(page: Page) {
  await page.waitForTimeout(3_000);
  for (let i = 0; i < 5; i++) {
    const host = page.frames().find((f) => /JqueryModalDialogs\.html/i.test(f.url()));
    const open = page.frames().some((f) => DIALOG.test(f.url()) || MEDIA_BROWSER.test(f.url()) || /MApp/i.test(f.url()));
    if (!host || !open) return;
    await host.evaluate(() => (window as any).dialogClose());
    await page.waitForTimeout(1_000);
  }
}

async function runInIse(page: Page, s: string) {
  await page.evaluate((code) => {
    const editor = (window as any).ace.edit(document.querySelector(".ace_editor") as HTMLElement);
    editor.setValue(code, -1);
  }, s);
  await page.locator(".ace_editor").first().click();
  await page.keyboard.press("Control+e");
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

for (const site of sites) {
  test.describe(`Read-Variable image editor - ${site.url}`, () => {
    test.use({ storageState: authStatePath(site) });

    test.afterAll(async ({ browser }) => {
      const context = await browser.newContext({ ignoreHTTPSErrors: true, storageState: authStatePath(site) });
      const page = await context.newPage();
      await openIse(page, site);
      // own session, so a dialog left open by a failed test cannot block the cleanup
      await page.evaluate(() => (window as any).scForm.postRequest("", "", "", "ise:setsessionid(id=read-variable-image-teardown)"));
      await runInIse(page, teardownScript);
      await expect(page.locator(".terminal-output")).toContainText("TEARDOWN_OK", { timeout: 60_000 });
      await sitecoreLogout(page, site);
      await context.close();
    });

    test("edits an image field value with the Content Editor Image field control", async ({ page }) => {
      test.setTimeout(180_000);
      await openIse(page, site);
      await runInIse(page, script);

      const dialog = await waitForFrame(page, DIALOG, 90_000);
      const editor = dialog.locator(".imageWrapper");
      const mediaPath = editor.locator("input.scContentControlImage");
      await expect(mediaPath).toHaveValue("/spe-test/ImageOne", { timeout: 15_000 });

      await editor.getByRole("link", { name: "Clear", exact: true }).click();
      await expect(mediaPath).toHaveValue("", { timeout: 15_000 });

      await editor.getByRole("link", { name: "Browse", exact: true }).click();
      const mediaBrowser = await waitForFrame(page, MEDIA_BROWSER, 30_000);
      await mediaBrowser.locator("span", { hasText: /^ImageTwo$/ }).first().click({ timeout: 60_000 });
      await mediaBrowser.getByRole("button", { name: "Select" }).click();
      await expect(mediaPath).toHaveValue("/spe-test/ImageTwo", { timeout: 15_000 });

      await dialog.locator("#OKButton").click();
      const terminal = page.locator(".terminal-output");
      await expect(terminal).toContainText("RESULT=ok", { timeout: 60_000 });
      // the ISE terminal keeps output of earlier runs; look at this run only
      const all = await terminal.innerText();
      const output = all.slice(all.lastIndexOf("RESULT=ok"));
      const second = output.match(/SECOND=(\{[0-9A-F-]{36}\})/i)?.[1];
      expect(second).toBeTruthy();
      expect(output).toMatch(new RegExp(`FIELD=<image\\s+mediaid="${second}"`, "i"));
      expect(output).not.toMatch(/\[\[;/);
    });

    // The Content Hub DAM connector swaps in its own Image field control; its menu buttons must reach it.
    test("routes DAM connector buttons to the connector's Image field control", async ({ page }) => {
      test.setTimeout(180_000);
      await openIse(page, site);
      await runInIse(page, script);

      const dialog = await waitForFrame(page, DIALOG, 90_000);
      const editor = dialog.locator(".imageWrapper");
      await expect(editor.locator("input.scContentControlImage")).toHaveValue("/spe-test/ImageOne", { timeout: 15_000 });
      const damBrowse = editor.getByRole("link", { name: "Browse Sitecore DAM", exact: true });
      if (!(await damBrowse.count())) {
        await dialog.locator("#CancelButton").click();
        test.skip(true, "DAM connector not installed");
      }

      await editor.getByRole("link", { name: "DAM Transformation", exact: true }).click();
      const alert = await waitForFrame(page, /xmlcontrol=Alert/i, 30_000);
      await expect(alert.locator("body")).toContainText("does not contain DAM assets details");
      await alert.getByRole("button", { name: "OK" }).click();

      await damBrowse.click();
      await waitForFrame(page, /client\/Applications\/MApp/i, 30_000);
      await closeOrphanedDialogs(page);
      await expect(page.locator(".terminal-output")).toContainText("RESULT=cancel", { timeout: 60_000 });
    });
  });
}
