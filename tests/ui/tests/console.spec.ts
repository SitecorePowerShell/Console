import { test, expect, Page } from "@playwright/test";
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
const errorScript = fs.readFileSync(
  path.join(__dirname, "console-error.ps1"),
  "utf-8"
);
const multilineScript = fs.readFileSync(
  path.join(__dirname, "console-multiline.ps1"),
  "utf-8"
);
const newlineScript = fs.readFileSync(
  path.join(__dirname, "console-newline.ps1"),
  "utf-8"
);
const fastScript = fs.readFileSync(
  path.join(__dirname, "console-fast.ps1"),
  "utf-8"
);
const blankLineScript = fs.readFileSync(
  path.join(__dirname, "console-blankline.ps1"),
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

async function executeInConsole(page: Page, script: string) {
  await page.evaluate((s) => {
    const term = (window as any).jQuery("#terminal").terminal();
    term.exec(s);
  }, script);
}

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
      await waitForConsoleReady(page);
      await executeInConsole(page, testScript);

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

    test("errors render as red text without black background", async ({
      page,
    }) => {
      await page.goto(
        `${site.url}/sitecore/shell/applications/powershell/powershell-console`,
        { waitUntil: "domcontentloaded" }
      );
      await waitForConsoleReady(page);
      await executeInConsole(page, errorScript);

      await expect(page.locator(".terminal-output")).toContainText(
        "missing the terminator",
        { timeout: 30_000 }
      );

      // No error span should have a black background
      const styledSpans = page.locator('.terminal-output span[style*="background-color"]');
      const count = await styledSpans.count();
      for (let i = 0; i < count; i++) {
        const style = await styledSpans.nth(i).getAttribute("style");
        expect(style).not.toMatch(/background-color:\s*#000/);
        expect(style).not.toMatch(/background-color:\s*rgb\(0,\s*0,\s*0\)/);
      }
    });

    test("multi-line script with foreach executes and returns prompt", async ({
      page,
    }) => {
      await page.goto(
        `${site.url}/sitecore/shell/applications/powershell/powershell-console`,
        { waitUntil: "domcontentloaded" }
      );
      await waitForConsoleReady(page);
      await executeInConsole(page, multilineScript);

      await expect(page.locator(".terminal-output")).toContainText("Done!", {
        timeout: 30_000,
      });

      const output = await page.locator(".terminal-output").innerText();

      // All 15 color rows rendered
      const lines = output.split("\n").filter((l) => l.includes("test"));
      expect(lines.length).toBe(15);

      // No format artifacts
      expect(output).not.toMatch(/\[\[;/);

      expect(output).toContain("Done!");
    });

    test("blank line separates output from prompt", async ({ page }) => {
      await page.goto(
        `${site.url}/sitecore/shell/applications/powershell/powershell-console`,
        { waitUntil: "domcontentloaded" }
      );
      await waitForConsoleReady(page);
      await executeInConsole(page, blankLineScript);

      await expect(page.locator(".terminal-output")).toContainText(
        "output line",
        { timeout: 30_000 }
      );

      // Wait for the terminal to be unpaused (prompt returned)
      await page.waitForFunction(
        () => {
          const term = (window as any).jQuery("#terminal").terminal();
          return term && !term.paused();
        },
        { timeout: 10_000 }
      );

      // The last div in terminal-output should be the blank separator
      // line (a space character) between script output and prompt.
      const lastDiv = page.locator(".terminal-output > div").last();
      const lastText = await lastDiv.innerText();
      expect(lastText.trim()).toBe("");
    });

    test("text with embedded newlines renders without jsterm artifacts", async ({
      page,
    }) => {
      await page.goto(
        `${site.url}/sitecore/shell/applications/powershell/powershell-console`,
        { waitUntil: "domcontentloaded" }
      );
      await waitForConsoleReady(page);
      await executeInConsole(page, newlineScript);

      await expect(page.locator(".terminal-output")).toContainText("Done!", {
        timeout: 30_000,
      });

      const output = (await page.locator(".terminal-output").innerText()).replace(/\u00A0/g, " ");
      expect(output).toContain("Sample line starting with EOL char");
      expect(output).not.toMatch(/\[\[;/);
    });

    test("fast script returns prompt without hanging", async ({ page }) => {
      await page.goto(
        `${site.url}/sitecore/shell/applications/powershell/powershell-console`,
        { waitUntil: "domcontentloaded" }
      );
      await waitForConsoleReady(page);

      // Run a trivially fast script 3 times in succession. If the
      // fast-completion polling path is broken, the terminal will hang
      // on the first or second execution and never become unpaused.
      for (let i = 0; i < 3; i++) {
        await executeInConsole(page, fastScript);
        await expect(page.locator(".terminal-output")).toContainText("fast", {
          timeout: 15_000,
        });
        await page.waitForFunction(
          () => {
            const term = (window as any).jQuery("#terminal").terminal();
            return term && !term.paused();
          },
          { timeout: 15_000 }
        );
      }
    });
  });
}
