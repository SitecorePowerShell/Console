import { test, expect, Page } from "@playwright/test";
import { loadTestSites, authStatePath, sitecoreLogout } from "./sitecore-login";

const sites = loadTestSites();

const SPLITTERS = {
  horizontal: { handle: "#ResultsSplitter", editor: "#ScriptPane", output: "#Result", axis: "y" as const },
  vertical: { handle: "#VerticalSplitter", editor: "#VerticalLeftPane", output: "#VerticalRightPane", axis: "x" as const },
};

async function rect(page: Page, selector: string) {
  return page.locator(selector).evaluate((el) => {
    const r = el.getBoundingClientRect();
    return { x: r.left, y: r.top, w: r.width, h: r.height };
  });
}

for (const site of sites) {
  test.describe(`ISE editor/output splitter - ${site.url}`, () => {
    test.use({ storageState: authStatePath(site) });

    test.afterAll(async ({ browser }) => {
      const context = await browser.newContext({ ignoreHTTPSErrors: true, storageState: authStatePath(site) });
      const page = await context.newPage();
      await sitecoreLogout(page, site);
      await context.close();
    });

    for (const [orientation, s] of Object.entries(SPLITTERS)) {
      test(`${orientation}: split lands where the drag ghost was, within pane minimums`, async ({ page }) => {
        await page.setViewportSize({ width: 1400, height: 900 });
        const url = `${site.url}/sitecore/shell/Applications/PowerShell/PowerShellIse`;
        await page.goto(url, { waitUntil: "domcontentloaded" });
        await page.waitForSelector("#CodeEditor1", { timeout: 15_000 });
        await page.evaluate((o) => {
          localStorage.setItem("spe::ise.splitOrientation", o);
          localStorage.removeItem("spe::ise.splitPosition");
        }, orientation);
        await page.reload({ waitUntil: "domcontentloaded" });
        await page.waitForSelector("#CodeEditor1", { timeout: 15_000 });
        await page.waitForSelector(s.handle, { state: "visible", timeout: 15_000 });

        // Far past each end of the range, then a point in the middle.
        for (const target of [5000, -5000, 450]) {
          const h = await rect(page, s.handle);
          const from = { x: h.x + h.w / 2, y: h.y + h.h / 2 };
          const to = s.axis === "y" ? { x: from.x, y: Math.max(1, target) } : { x: Math.max(1, target), y: from.y };
          await page.mouse.move(from.x, from.y);
          await page.mouse.down();
          await page.mouse.move(Math.min(to.x, 1399), Math.min(to.y, 899), { steps: 10 });
          const ghost = await rect(page, ".spe-split-ghost");
          await page.mouse.up();
          await expect(page.locator(".spe-split-ghost")).toHaveCount(0);
          await page.waitForTimeout(200);

          const after = await rect(page, s.handle);
          const ghostPos = s.axis === "y" ? ghost.y : ghost.x;
          const afterPos = s.axis === "y" ? after.y : after.x;
          expect(Math.abs(afterPos - ghostPos), `target ${target}`).toBeLessThanOrEqual(1);

          const editor = await rect(page, s.editor);
          const output = await rect(page, s.output);
          const size = (r: { w: number; h: number }) => (s.axis === "y" ? r.h : r.w);
          expect(size(editor), `editor at target ${target}`).toBeGreaterThanOrEqual(195);
          expect(size(output), `output at target ${target}`).toBeGreaterThanOrEqual(90);
          const status = await rect(page, "#StatusBar");
          expect(status.y + status.h).toBeLessThanOrEqual(900);
        }

        await page.evaluate(() => localStorage.removeItem("spe::ise.splitOrientation"));
      });
    }

    test("vertical: tabs span the editor and output, and move back on toggle", async ({ page }) => {
      await page.setViewportSize({ width: 1400, height: 900 });
      const url = `${site.url}/sitecore/shell/Applications/PowerShell/PowerShellIse`;
      await page.goto(url, { waitUntil: "domcontentloaded" });
      await page.waitForSelector("#CodeEditor1", { timeout: 15_000 });
      await page.evaluate(() => localStorage.setItem("spe::ise.splitOrientation", "vertical"));
      await page.reload({ waitUntil: "domcontentloaded" });
      await page.waitForSelector("#VerticalSplitter", { state: "visible", timeout: 15_000 });

      const tabs = await rect(page, "#TabsPanel");
      const left = await rect(page, "#VerticalLeftPane");
      const right = await rect(page, "#VerticalRightPane");
      expect(Math.abs(tabs.x - left.x)).toBeLessThanOrEqual(1);
      expect(Math.abs(tabs.x + tabs.w - (right.x + right.w))).toBeLessThanOrEqual(1);
      expect(left.y).toBeGreaterThanOrEqual(tabs.y + tabs.h - 1);
      expect(right.y).toBeGreaterThanOrEqual(tabs.y + tabs.h - 1);
      const editor = await rect(page, "#CodeEditors");
      expect(editor.y + editor.h).toBeLessThanOrEqual(left.y + left.h + 1);

      await page.locator("#ToggleSplitOrientation").click();
      await expect(page.locator("#EditingArea > #TabsPanel")).toHaveCount(1);
      await expect(page.locator("#VerticalSplitContainer")).toHaveCount(0);

      await page.evaluate(() => localStorage.removeItem("spe::ise.splitOrientation"));
    });
  });
}
