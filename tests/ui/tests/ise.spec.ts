import { test, expect, Page } from "@playwright/test";
import {
  loadTestSites,
  authStatePath,
  sitecoreLogout,
} from "./sitecore-login";
import * as fs from "fs";
import * as path from "path";

const sites = loadTestSites();
const noNewlineScript = fs.readFileSync(
  path.join(__dirname, "ise-nonewline.ps1"),
  "utf-8"
);
const writeHostScript = fs.readFileSync(
  path.join(__dirname, "ise-writehost.ps1"),
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
  test.describe(`ISE output - ${site.url}`, () => {
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

    test("NoNewline and paths render correctly", async ({ page }) => {
      await setEditorAndRun(page, noNewlineScript);

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

    test("Write-Host with empty lines and sections", async ({ page }) => {
      await setEditorAndRun(page, writeHostScript);

      const terminal = page.locator(".terminal-output");
      await expect(terminal).toContainText("Footer line", {
        timeout: 90_000,
      });

      await expect(terminal).toContainText("Header line");
      await expect(terminal).toContainText("Section one");
      await expect(terminal).toContainText("Section two");
      await expect(terminal).toContainText("Footer line");
      await expect(terminal).toContainText("Child1");
      await expect(terminal).toContainText("Child2");

      const output = await terminal.innerText();
      expect(output).not.toMatch(/\[\[;#[A-F0-9]+;#[A-F0-9]+/);
    });
  });
}
