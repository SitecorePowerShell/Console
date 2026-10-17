import { test, expect, Page } from "@playwright/test";
import {
  loadTestSites,
  authStatePath,
  sitecoreLogout,
} from "./sitecore-login";
import * as fs from "fs";
import * as path from "path";

const sites = loadTestSites();
const findItemScript = fs.readFileSync(
  path.join(__dirname, "find-item.ps1"),
  "utf-8"
);

async function waitForConsoleReady(page: Page) {
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
}

for (const site of sites) {
  test.describe(`Find-Item - ${site.url}`, () => {
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

    test("-Template works on indexes without the _templates field", async ({ page }) => {
      test.setTimeout(150_000);
      await page.goto(
        `${site.url}/sitecore/shell/applications/powershell/powershell-console`,
        { waitUntil: "domcontentloaded" }
      );
      await waitForConsoleReady(page);
      await page.evaluate((s) => {
        (window as any).jQuery("#terminal").terminal().exec(s);
      }, findItemScript);

      await expect(page.locator(".terminal-output")).toContainText("Done!", {
        timeout: 120_000,
      });

      const output = await page.locator(".terminal-output").innerText();
      expect(output).toMatch(/Indexed:\s+1/);
      expect(output).toMatch(/Single\s+template:\s+1/);
      expect(output).toMatch(/Multiple\s+templates:\s+1/);
      expect(output).toMatch(/Other\s+template:\s+0/);
      expect(output).not.toMatch(/\[\[;/);
    });
  });
}
