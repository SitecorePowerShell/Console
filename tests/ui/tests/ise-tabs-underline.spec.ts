import { test, expect } from "@playwright/test";
import { loadTestSites, authStatePath, sitecoreLogout } from "./sitecore-login";

const sites = loadTestSites();

for (const site of sites.slice(0, 1)) {
  test.describe(`ISE tabs underline diag - ${site.url}`, () => {
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

    test("capture tabs strip at 1920x1080", async ({ page }) => {
      await page.setViewportSize({ width: 1920, height: 1080 });
      await page.goto(
        `${site.url}/sitecore/shell/Applications/PowerShell/PowerShellIse`,
        { waitUntil: "domcontentloaded" }
      );
      await page.waitForSelector("#CodeEditor1", { timeout: 15_000 });
      await page.waitForTimeout(1500);

      // Screenshot just the top tabs strip area.
      await page.screenshot({
        path: ".playwright-mcp/ise-tabs-strip.png",
        clip: { x: 0, y: 100, width: 1920, height: 80 },
      });

      const info = await page.evaluate(() => {
        const probe = (sel: string) => {
          const el = document.querySelector(sel) as HTMLElement | null;
          if (!el) return null;
          const rect = el.getBoundingClientRect();
          const cs = window.getComputedStyle(el);
          return {
            tag: el.tagName,
            id: el.id,
            cls: el.className,
            rect: {
              l: Math.round(rect.left),
              t: Math.round(rect.top),
              w: Math.round(rect.width),
              h: Math.round(rect.height),
            },
            width: cs.width,
            marginLeft: cs.marginLeft,
            borderBottom: cs.borderBottom,
            paddingLeft: cs.paddingLeft,
          };
        };
        return {
          tabsPanel: probe("#TabsPanel"),
          tabstrip: probe("#Tabs"),
          tabstripContainer: probe(".scTabstrip"),
          firstNobr: probe("nobr:first-child"),
          firstTab: probe(".scTab, .scTabActive, .scTab_Hover, .scTabActive_Hover"),
          treeToggle: probe("#TreeViewToggle"),
          varToggle: probe("#VariablesToggle"),
        };
      });
      console.log(JSON.stringify(info, null, 2));

      // Black underline at the bottom of the tabs strip must span the viewport.
      expect(info.tabstripContainer?.rect.w).toBe(1920);
      expect(info.tabstripContainer?.borderBottom).toContain("2px solid");
    });
  });
}
