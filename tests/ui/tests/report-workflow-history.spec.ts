import { test, expect, Page } from "@playwright/test";
import {
  loadTestSites,
  authStatePath,
  sitecoreLogout,
} from "./sitecore-login";
import * as fs from "fs";
import * as path from "path";

const sites = loadTestSites();
const reportScript = fs.readFileSync(
  path.join(__dirname, "report-workflow-history.ps1"),
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
  test.describe(`Recent workflow history report - ${site.url}`, () => {
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

    test.beforeEach(async ({ page }) => {
      await page.goto(
        `${site.url}/sitecore/shell/Applications/PowerShell/PowerShellIse`,
        { waitUntil: "domcontentloaded" }
      );
      await page.waitForSelector(".ace_editor", { timeout: 15_000 });
    });

    test("lists item and branch history across versions and periods", async ({ page }) => {
      await setEditorAndRun(page, reportScript);

      const terminal = page.locator(".terminal-output");
      await expect(terminal).toContainText("Done!", { timeout: 90_000 });

      const output = await terminal.innerText();
      expect(output).toContain("ItemNewestFirst=True");
      expect(output).toContain("RemovedVersionListed=True");
      expect(output).toContain("ExistingVersionFlagged=True");
      expect(output).toContain("StateNames=True");
      expect(output).toContain("ChangedFields=True");
      expect(output).toContain("BranchNewestFirst=True");
      expect(output).toContain("PastPeriodEmpty=True");
      expect(output).toContain("CurrentPeriodCount=5");
      expect(output).not.toMatch(/\[\[;/);
    });
  });
}
