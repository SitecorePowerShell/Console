import { test, expect, Page } from "@playwright/test";
import {
  loadTestSites,
  authStatePath,
  sitecoreLogout,
} from "./sitecore-login";
import * as fs from "fs";
import * as path from "path";

const sites = loadTestSites();
const enableSaveScript = fs.readFileSync(
  path.join(__dirname, "ise-tabs-enable-save.ps1"),
  "utf-8"
);
const disableSaveScript = fs.readFileSync(
  path.join(__dirname, "ise-tabs-disable-save.ps1"),
  "utf-8"
);

// Select tab 1 and close every other tab so the visible editor is
// #CodeEditor1 regardless of what earlier tests or tab restore left behind.
async function normalizeTabs(page: Page) {
  await page.waitForFunction(
    () => document.querySelectorAll("[id^='Tabs_tab_']").length >= 1,
    { timeout: 20_000 }
  );
  await page.evaluate(() => {
    (window as any).spe.changeTab(1);
  });
  const extras = (await page.locator("[id^='Tabs_tab_']").count()) - 1;
  for (let idx = extras + 1; idx >= 2; idx--) {
    await page.evaluate((i) => {
      (window as any).scForm.postRequest("", "", "", `ise:closetab(index=${i},modified=false,selectIndex=1)`);
    }, idx);
    await page.waitForFunction(
      (expected) => document.querySelectorAll("[id^='Tabs_tab_']").length === expected,
      idx - 1,
      { timeout: 10_000 }
    );
  }
  await page.waitForSelector("#CodeEditor1:not([style*='display: none'])", {
    timeout: 15_000,
  });
}

async function runScript(page: Page, script: string, expectText: string) {
  await page.evaluate((s) => {
    const editor = (window as any).ace.edit(
      document.querySelector("#CodeEditor1") as HTMLElement
    );
    editor.setValue(s, -1);
  }, script);
  await page.keyboard.press("Control+e");
  await expect(page.locator(".terminal-output")).toContainText(expectText, {
    timeout: 90_000,
  });
}

for (const site of sites) {
  test.describe(`ISE tabs - ${site.url}`, () => {
    test.use({ storageState: authStatePath(site) });

    test.afterAll(async ({ browser }) => {
      const context = await browser.newContext({
        ignoreHTTPSErrors: true,
        storageState: authStatePath(site),
      });
      const page = await context.newPage();
      await sitecoreLogout(page, site);
      await page.close();
      await context.close();
    });

    test("middle-click closes a tab", async ({ page }) => {
      await page.goto(
        `${site.url}/sitecore/shell/Applications/PowerShell/PowerShellIse`,
        { waitUntil: "domcontentloaded" }
      );
      await page.waitForSelector(".ace_editor", { timeout: 15_000 });

      // Should start with one tab
      const initialTabs = await page.locator("[id^='Tabs_tab_']").count();
      expect(initialTabs).toBe(1);

      // Open a second tab via Sitecore command
      await page.evaluate(() => {
        (window as any).scForm.postRequest("", "", "", "ise:new");
      });
      await page.waitForFunction(
        () => document.querySelectorAll("[id^='Tabs_tab_']").length === 2,
        { timeout: 10_000 }
      );

      // Middle-click the second (new, unmodified) tab to close it.
      // Closing an unmodified tab skips the save confirmation dialog.
      const secondTab = page.locator("#Tabs_tab_1");
      await secondTab.click({ button: "middle" });

      // Wait for tab count to drop back to 1
      await page.waitForFunction(
        () => document.querySelectorAll("[id^='Tabs_tab_']").length === 1,
        { timeout: 10_000 }
      );

      const finalTabs = await page.locator("[id^='Tabs_tab_']").count();
      expect(finalTabs).toBe(1);
    });

    test("open tabs are restored after reload", async ({ page }) => {
      const iseUrl = `${site.url}/sitecore/shell/Applications/PowerShell/PowerShellIse`;
      await page.goto(iseUrl, { waitUntil: "domcontentloaded" });
      await normalizeTabs(page);

      // Restore only runs when the user's SaveActiveTabs setting is on. It is
      // switched back off in the finally block so the other specs, which
      // assume a single clean tab on load, keep working.
      await runScript(page, enableSaveScript, "SaveActiveTabs after: True");
      try {
        await page.goto(iseUrl, { waitUntil: "domcontentloaded" });
        await normalizeTabs(page);

        // Open a second tab and give it content so the client includes it in
        // the persisted state (untitled tabs with no content are dropped).
        await page.evaluate(() => {
          (window as any).scForm.postRequest("", "", "", "ise:new");
        });
        await page.waitForFunction(
          () => document.querySelectorAll("[id^='Tabs_tab_']").length === 2,
          { timeout: 10_000 }
        );
        const marker = "Write-Host 'restored <tab>'";
        const saved = page.waitForResponse((r) =>
          (r.request().postData() || "").includes("savetabstate")
        );
        await page.evaluate((text) => {
          const w = window as any;
          w.ace.edit(document.querySelector("#CodeEditor2") as HTMLElement).setValue(text, -1);
          w.spe.saveActiveTabs();
        }, marker);
        await saved;

        // The server deserializes the saved state and pushes it back on load.
        await page.goto(iseUrl, { waitUntil: "domcontentloaded" });
        await page.waitForFunction(
          () => document.querySelectorAll("[id^='Tabs_tab_']").length === 2,
          { timeout: 20_000 }
        );
        await page.waitForFunction(
          (text) => {
            const el = document.querySelector("#CodeEditor2") as HTMLElement | null;
            return !!el && (window as any).ace.edit(el).getValue() === text;
          },
          marker,
          { timeout: 15_000 }
        );
      } finally {
        await page.goto(iseUrl, { waitUntil: "domcontentloaded" });
        await normalizeTabs(page);
        await runScript(page, disableSaveScript, "SaveActiveTabs after: False");
      }
    });
  });
}
