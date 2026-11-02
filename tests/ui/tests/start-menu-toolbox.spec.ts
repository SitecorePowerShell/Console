import { test, expect } from "@playwright/test";
import { loadTestSites, authStatePath, sitecoreLogout } from "./sitecore-login";

const sites = loadTestSites();

for (const site of sites) {
  test.describe(`Start menu PowerShell Toolbox - ${site.url}`, () => {
    test.use({ storageState: authStatePath(site) });

    test.afterAll(async ({ browser }) => {
      const context = await browser.newContext({ ignoreHTTPSErrors: true, storageState: authStatePath(site) });
      const page = await context.newPage();
      await sitecoreLogout(page, site);
      await context.close();
    });

    test("renders as a folder and opens its menu to the right", async ({ page }) => {
      await page.goto(`${site.url}/sitecore/shell/default.aspx`, { waitUntil: "domcontentloaded" });
      await page.waitForFunction(() => (window as any).scManager, null, { timeout: 30_000 });
      await page.locator("#StartButton").click();

      // showPopup displays a copy of the hidden #StartMenu inside .scPopup
      const toolbox = page.locator(".scPopup .scStartMenuRight a.scStartMenuRightFolder[onclick*='integrationPoint=toolbox']");
      await expect(toolbox).toBeVisible({ timeout: 15_000 });
      await expect(toolbox.locator("img.scStartMenuRightFolderGlyph")).toHaveCount(1);
      await expect(toolbox).toHaveId(/.+/);

      // Click near the left edge so a cursor-anchored menu would land well left of the item's right edge.
      const item = (await toolbox.boundingBox())!;
      await toolbox.click({ position: { x: 10, y: item.height / 2 } });

      const popup = page.locator(".scPopup").last();
      await expect(popup).toBeVisible({ timeout: 15_000 });
      const box = (await popup.boundingBox())!;
      expect(box.x).toBeGreaterThan(item.x + item.width - 10);
      // Sitecore shifts tall menus up to fit the viewport, so only require the menu to span the item's row
      expect(box.y).toBeLessThan(item.y + 5);
      expect(box.y + box.height).toBeGreaterThan(item.y + item.height - 5);
    });
  });
}
