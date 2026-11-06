import { test, expect, Page, Frame } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";
import { loadTestSites, authStatePath, refreshSitecoreSession, TestSite } from "./sitecore-login";

const sites = loadTestSites();
const script = fs.readFileSync(path.join(__dirname, "read-variable-general-link.ps1"), "utf-8");
const teardownScript = fs.readFileSync(path.join(__dirname, "read-variable-general-link-teardown.ps1"), "utf-8");

const DIALOG = /PowerShellMultiValuePrompt/i;
// the modal host URL embeds the dialog URL, so match the inner page only
const EXTERNAL_LINK = /^(?!.*JqueryModalDialogs).*Dialogs\/External%20link\.aspx/i;

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
    const open = page.frames().some((f) => DIALOG.test(f.url()) || EXTERNAL_LINK.test(f.url()) || /MApp/i.test(f.url()));
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
  test.describe(`Read-Variable general link editor - ${site.url}`, () => {
    test.use({ storageState: authStatePath(site) });

    test.afterAll(async ({ browser }) => {
      const context = await browser.newContext({ ignoreHTTPSErrors: true, storageState: authStatePath(site) });
      const page = await context.newPage();
      await openIse(page, site);
      // own session, so a dialog left open by a failed test cannot block the cleanup
      await page.evaluate(() => (window as any).scForm.postRequest("", "", "", "ise:setsessionid(id=read-variable-general-link-teardown)"));
      await runInIse(page, teardownScript);
      await expect(page.locator(".terminal-output")).toContainText("TEARDOWN_OK", { timeout: 60_000 });
      await refreshSitecoreSession(page, site);
      await context.close();
    });

    test("edits a General Link field value with the Content Editor Link field control", async ({ page }) => {
      test.setTimeout(180_000);
      await openIse(page, site);
      await runInIse(page, script);

      const dialog = await waitForFrame(page, DIALOG, 90_000);
      const editor = dialog.locator(".linkWrapper");
      const linkPath = editor.locator("input");
      await expect(linkPath).toHaveValue("/spe-test/LinkTarget", { timeout: 15_000 });

      await editor.getByRole("link", { name: "Clear", exact: true }).click();
      await expect(linkPath).toHaveValue("", { timeout: 15_000 });

      await editor.getByRole("link", { name: /external link/i }).click();
      const external = await waitForFrame(page, EXTERNAL_LINK, 30_000);
      await external.locator("#Text").fill("spe docs");
      await external.locator("#Url").fill("https://doc.sitecorepowershell.com/");
      await external.getByRole("button", { name: "Insert" }).click();
      await expect(linkPath).toHaveValue("https://doc.sitecorepowershell.com/", { timeout: 15_000 });

      await dialog.locator("#OKButton").click();
      const terminal = page.locator(".terminal-output");
      await expect(terminal).toContainText("RESULT=ok", { timeout: 60_000 });
      // the ISE terminal keeps output of earlier runs; look at this run only
      const all = await terminal.innerText();
      const output = all.slice(all.lastIndexOf("RESULT=ok"));
      expect(output).toMatch(/FIELD=<link[^>]*linktype="external"/i);
      expect(output).toMatch(/url="https:\/\/doc\.sitecorepowershell\.com\/"/i);
      expect(output).toMatch(/text="spe\sdocs"/i);
      expect(output).not.toMatch(/\[\[;/);
    });
    // The Content Hub DAM connector adds an Insert DAM link button; it must reach the connector's handler.
    test("routes the DAM connector button to the connector's link picker", async ({ page }) => {
      test.setTimeout(180_000);
      await openIse(page, site);
      await runInIse(page, script);

      const dialog = await waitForFrame(page, DIALOG, 90_000);
      const editor = dialog.locator(".linkWrapper");
      await expect(editor.locator("input")).toHaveValue("/spe-test/LinkTarget", { timeout: 15_000 });
      const damLink = editor.getByRole("link", { name: "Insert DAM link", exact: true });
      if (!(await damLink.count())) {
        await dialog.locator("#CancelButton").click();
        test.skip(true, "DAM connector not installed");
      }

      await damLink.click();
      await waitForFrame(page, /client\/Applications\/MApp/i, 30_000);
      await closeOrphanedDialogs(page);
      await expect(page.locator(".terminal-output")).toContainText("RESULT=cancel", { timeout: 60_000 });
    });
  });
}
