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
const errorScript = fs.readFileSync(
  path.join(__dirname, "ise-error.ps1"),
  "utf-8"
);
const clearHostScript = fs.readFileSync(
  path.join(__dirname, "ise-clearhost.ps1"),
  "utf-8"
);
const newlineScript = fs.readFileSync(
  path.join(__dirname, "ise-newline.ps1"),
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

    test("script errors render as red text without HTML artifacts", async ({
      page,
    }) => {
      await setEditorAndRun(page, errorScript);

      const terminal = page.locator(".terminal-output");

      // Wait for error output to appear
      await expect(terminal).toContainText("missing the terminator", {
        timeout: 30_000,
      });

      const output = await terminal.innerText();

      // No raw HTML tags should appear in visible output
      expect(output).not.toMatch(/<pre[\s>]/i);
      expect(output).not.toMatch(/<span[\s>]/i);
      expect(output).not.toMatch(/<br\s*\/?>/i);
    });

    test("Clear-Host clears terminal output", async ({ page }) => {
      // First run a script that produces output
      await setEditorAndRun(page, 'Write-Host "before clear"');

      const terminal = page.locator(".terminal-output");
      await expect(terminal).toContainText("before clear", {
        timeout: 30_000,
      });

      // Now run a script with Clear-Host at the top
      await setEditorAndRun(page, clearHostScript);

      await expect(terminal).toContainText("after clear", {
        timeout: 30_000,
      });

      // "before clear" should be gone - Clear-Host wiped it.
      // jquery.terminal uses &nbsp; for spaces, which innerText()
      // returns as \u00A0. Normalize to regular spaces for comparison.
      const output = (await terminal.innerText()).replace(/\u00A0/g, " ");
      expect(output).toContain("after clear");
      expect(output).not.toContain("before clear");
    });

    test("text with embedded newlines renders without jsterm artifacts", async ({
      page,
    }) => {
      await setEditorAndRun(page, newlineScript);

      const terminal = page.locator(".terminal-output");
      await expect(terminal).toContainText("Done!", { timeout: 30_000 });

      const output = (await terminal.innerText()).replace(/\u00A0/g, " ");
      expect(output).toContain("Sample line starting with EOL char");
      expect(output).toContain("Done!");
      expect(output).not.toMatch(/\[\[;/);

      // The script has two \n after the text - there should be blank
      // lines between "EOL char" and "Done!"
      const lines = output.split("\n");
      const eolIdx = lines.findIndex((l) => l.includes("EOL char"));
      const doneIdx = lines.findIndex((l) => l.includes("Done!"));
      expect(doneIdx - eolIdx).toBeGreaterThanOrEqual(3);
    });
  });
}
