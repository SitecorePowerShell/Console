import { test, expect, Page } from "@playwright/test";
import {
  loadTestSites,
  authStatePath,
  sitecoreLogout,
} from "./sitecore-login";
import * as fs from "fs";
import * as path from "path";

const sites = loadTestSites();
const debugScript = fs.readFileSync(
  path.join(__dirname, "ise-debug-terminal.ps1"),
  "utf-8"
);

for (const site of sites) {
  test.describe(`ISE debug terminal - ${site.url}`, () => {
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

    test("terminal command at breakpoint produces output (#1067 regression)", async ({
      page,
    }) => {
      await page.evaluate((s) => {
        const editor = (window as any).ace.edit(
          document.querySelector(".ace_editor") as HTMLElement
        );
        editor.setValue(s, -1);
        editor.focus();
      }, debugScript);

      // Trigger the same Sheer message Ctrl+D dispatches in ise.js
      // ("debugScript" keybind -> scForm.postRequest "ise:debug").
      await page.evaluate(() => {
        (window as any).scForm.postRequest("", "", "", "ise:debug");
      });

      // Wait until the debugger pauses on Wait-Debugger.
      await page.waitForSelector(".ace_marker-layer .breakpoint", {
        timeout: 30_000,
      });

      const marker = "XYZdiag1067-from-terminal";
      const cmd = `Write-Host '${marker}'`;

      await page.locator("#ScriptResultCode .cmd").click();
      await page.keyboard.type(cmd, { delay: 10 });
      await page.keyboard.press("Enter");

      const terminal = page.locator(".terminal-output");
      // jquery.terminal echoes the typed command locally, so the marker
      // appears once before the server streams the result. Poll until the
      // marker count exceeds the echo count - that proves the result
      // streamed back through the breakpoint-routing path.
      await expect
        .poll(
          async () => {
            const t = (await terminal.innerText()).replace(/ /g, " ");
            const echo = (t.match(new RegExp("Write-Host", "g")) || []).length;
            const total = (t.match(new RegExp(marker, "g")) || []).length;
            return total > echo;
          },
          { timeout: 15_000, intervals: [500] }
        )
        .toBe(true);

      const outputText = (await terminal.innerText()).replace(/ /g, " ");
      expect(outputText).not.toContain("A Script is already executing");

      // Resume so the script finishes and the session is freed.
      await page.evaluate(() => {
        (window as any).scForm.postRequest(
          "",
          "",
          "",
          "ise:debugaction(action=Continue)"
        );
      });

      await expect(terminal).toContainText("after-resume-XYZdiag1067", {
        timeout: 30_000,
      });
    });

    test.afterEach(async ({ page }) => {
      // Best-effort safety: if the test failed mid-pause, try to release the
      // debugger so the next test (and the session) is not left hanging.
      try {
        await page.evaluate(() => {
          if ((window as any).scForm) {
            (window as any).scForm.postRequest(
              "",
              "",
              "",
              "ise:debugaction(action=Quit)"
            );
          }
        });
      } catch {
        // page may already be closed
      }
    });
  });
}
