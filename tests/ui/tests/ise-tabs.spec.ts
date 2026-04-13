import { test, expect } from "@playwright/test";
import {
  loadTestSites,
  authStatePath,
  sitecoreLogout,
} from "./sitecore-login";

const sites = loadTestSites();

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
  });
}
