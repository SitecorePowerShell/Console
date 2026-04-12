import { test, expect } from "@playwright/test";
import {
  loadTestSites,
  authStatePath,
  sitecoreLogout,
} from "./sitecore-login";
import * as fs from "fs";
import * as path from "path";

const sites = loadTestSites();
const testScript = fs.readFileSync(
  path.join(__dirname, "console.ps1"),
  "utf-8"
);

for (const site of sites) {
  test.describe(`Console output - ${site.url}`, () => {
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

    test("script with NoNewline and paths", async ({ page }) => {
      await page.goto(
        `${site.url}/sitecore/shell/applications/powershell/powershell-console`,
        { waitUntil: "domcontentloaded" }
      );

      // Wait for terminal to be ready
      await page.waitForFunction(
        () => {
          const jq = (window as any).jQuery;
          if (!jq) return false;
          const t = jq("#terminal");
          if (!t.length) return false;
          const term = t.terminal();
          return term && !term.paused();
        },
        { timeout: 60_000 }
      );

      await page.evaluate((script) => {
        const term = (window as any).jQuery("#terminal").terminal();
        term.exec(script);
      }, testScript);

      await expect(page.locator(".terminal-output")).toContainText("Done!", {
        timeout: 90_000,
      });

      const output = await page.locator(".terminal-output").innerText();

      expect(output).toContain("master:\\content\\spe-test");
      expect(output).toContain("master:\\content");
      expect(output).toMatch(/1\s+2\s+3/);
      expect(output).toMatch(/19\s+20/);
      expect(output).not.toMatch(/\[\[;/);
      expect(output).toContain("Done!");
    });
  });
}
