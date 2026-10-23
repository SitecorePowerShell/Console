import { test, expect, Page } from "@playwright/test";
import {
  loadTestSites,
  authStatePath,
  sitecoreLogout,
} from "./sitecore-login";
import * as fs from "fs";
import * as path from "path";

const sites = loadTestSites();
const progressScript = fs.readFileSync(
  path.join(__dirname, "ise-progress.ps1"),
  "utf-8"
);
const longOutputScript = fs.readFileSync(
  path.join(__dirname, "ise-progress-long.ps1"),
  "utf-8"
);
const indeterminateScript = fs.readFileSync(
  path.join(__dirname, "ise-progress-indeterminate.ps1"),
  "utf-8"
);

async function setEditorAndRun(page: Page, script: string) {
  await page.evaluate((s) => {
    const editor = (window as any).ace.edit(
      document.querySelector(".ace_editor") as HTMLElement
    );
    editor.setValue(s, -1);
  }, script);
  await page.keyboard.press("Control+e");
}

for (const site of sites) {
  test.describe(`ISE progress - ${site.url}`, () => {
    test.use({ storageState: authStatePath(site) });

    test.beforeEach(async ({ page }) => {
      await page.setViewportSize({ width: 1400, height: 900 });
      await page.goto(
        `${site.url}/sitecore/shell/Applications/PowerShell/PowerShellIse`,
        { waitUntil: "domcontentloaded" }
      );
      await page.waitForSelector(".ace_editor", { timeout: 15_000 });
      // Start from the default display options.
      await page.evaluate(() => localStorage.removeItem("spe.ise.progress"));
      await page.reload({ waitUntil: "domcontentloaded" });
      await page.waitForSelector(".ace_editor", { timeout: 15_000 });
    });

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

    test("progress panel pushes output down instead of covering it", async ({ page }) => {
      await setEditorAndRun(page, progressScript);

      const panel = page.locator("#IseProgress");
      await expect(panel).toBeVisible({ timeout: 60_000 });
      await expect(page.locator(".terminal-output")).toContainText("Third line of output");
      await expect(panel.locator(".spe-progress-activity")).toHaveText("Processing items");
      await expect(panel.locator(".spe-progress-row.spe-progress-status")).toContainText(/Item \d+ of 20/);
      await expect(panel.locator(".spe-progress-percent")).toHaveText(/^\d+%$/);
      await expect(panel.locator(".spe-progress-eta")).toContainText("remaining");
      await expect(panel.locator(".spe-progress-row.spe-progress-operation")).toContainText("master:\\content\\Home\\Item");
      await page.waitForTimeout(600); // let the terminal slide finish

      // Integrated (default): the first output line sits below the panel.
      const firstLine = page.locator(".terminal-output div", { hasText: "First line of output" }).first();
      const panelBox = (await panel.boundingBox())!;
      const lineBox = (await firstLine.boundingBox())!;
      expect(lineBox.y).toBeGreaterThanOrEqual(panelBox.y + panelBox.height - 1);
      // ...and the terminal shrinks instead of overflowing the results pane,
      // so there is no extra scrollbar that could scroll the panel away.
      const result = page.locator("#Result");
      const resultBox = (await result.boundingBox())!;
      const terminalBox = (await page.locator("#ScriptResultCode").boundingBox())!;
      expect(terminalBox.y + terminalBox.height).toBeLessThanOrEqual(resultBox.y + resultBox.height + 1);
      expect(await result.evaluate((e) => e.scrollHeight - e.clientHeight)).toBeLessThanOrEqual(0);

      // Gear menu shows the defaults: Hairline / Full / Integrated.
      await panel.hover();
      await panel.locator(".spe-progress-gear").click();
      await expect(panel).toHaveClass(/spe-progress-open/);
      await expect(panel.locator('button[aria-checked="true"]')).toHaveText(["Hairline", "Full", "Integrated"]);
      await expect(panel).toHaveClass(/spe-progress-bar-hairline/);
      await expect(panel).toHaveClass(/spe-progress-detail-full/);

      // The menu lingers after the mouse leaves, so it can still be reached.
      await page.mouse.move(5, 5);
      await page.waitForTimeout(500);
      await expect(panel).toHaveClass(/spe-progress-open/);

      // Overlay: the terminal goes back under the panel.
      await panel.locator('button[data-value="overlay"]').click();
      await expect(panel).toHaveClass(/spe-progress-placement-overlay/);
      await expect.poll(() => page.locator("#ScriptResultCode").evaluate((e) => e.style.top)).toBe("");
      expect(await page.evaluate(() => JSON.parse(localStorage.getItem("spe.ise.progress")!).placement)).toBe("overlay");
      await panel.locator('button[data-value="integrated"]').click();
      await expect.poll(() => page.locator("#ScriptResultCode").evaluate((e) => e.style.top)).not.toBe("");

      // Segmented: a segment fills in proportion to its share of the progress.
      await panel.locator('button[data-value="segmented"]').click();
      await expect(panel).toHaveClass(/spe-progress-bar-segmented/);
      await expect.poll(() => panel.locator(".spe-progress-segments i").evaluateAll((fills) =>
        fills.some((f) => { const w = parseFloat((f as HTMLElement).style.width); return w > 0 && w < 100; })
      ), { timeout: 10_000 }).toBe(true);
      await panel.locator('button[data-value="hairline"]').click();

      // ...and closes a moment later.
      await page.mouse.move(5, 5);
      await expect(panel).not.toHaveClass(/spe-progress-open/, { timeout: 3_000 });
      await expect(panel).not.toHaveClass(/spe-progress-active/);

      // The panel goes away when the script finishes and the terminal returns to the top.
      await expect(page.locator(".terminal-output")).toContainText("Progress done", { timeout: 60_000 });
      await expect(panel).toBeHidden();
      await expect.poll(() => page.locator("#ScriptResultCode").evaluate((e) => e.style.top)).toBe("");
      const output = await page.locator(".terminal-output").innerText();
      expect(output).not.toMatch(/\[\[;/);
    });

    test("newest output stays in view when the panel shrinks a full terminal", async ({ page }) => {
      await setEditorAndRun(page, longOutputScript);

      const panel = page.locator("#IseProgress");
      await expect(panel).toBeVisible({ timeout: 60_000 });
      await page.waitForTimeout(600); // let the terminal slide finish

      const terminalBox = (await page.locator("#ScriptResultCode").boundingBox())!;
      const lastLine = page.locator('[data-text^="Output line 60"]');
      const lineBox = (await lastLine.boundingBox())!;
      expect(lineBox.y).toBeGreaterThanOrEqual(terminalBox.y);
      expect(lineBox.y + lineBox.height).toBeLessThanOrEqual(terminalBox.y + terminalBox.height);

      await expect(page.locator(".terminal-output")).toContainText("Long output done", { timeout: 60_000 });
    });

    test("progress without a percentage shows an indeterminate bar", async ({ page }) => {
      await setEditorAndRun(page, indeterminateScript);

      const panel = page.locator("#IseProgress");
      await expect(panel).toBeVisible({ timeout: 60_000 });
      await expect(panel).toHaveClass(/spe-progress-indeterminate/);
      await expect(panel.locator(".spe-progress-percent")).toHaveText("");
      await expect(panel.locator(".spe-progress-row.spe-progress-status")).toContainText("items found");
      await expect(panel).toHaveClass(/spe-progress-no-operation/);

      // The sliding segment keeps moving.
      const marker = panel.locator(".spe-progress-hairline i");
      const first = await marker.evaluate((e) => e.style.left);
      await page.waitForTimeout(300);
      expect(await marker.evaluate((e) => e.style.left)).not.toBe(first);

      await expect(page.locator(".terminal-output")).toContainText("Scan done", { timeout: 60_000 });
      await expect(panel).toBeHidden();
    });
  });
}
