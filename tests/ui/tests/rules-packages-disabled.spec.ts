import { test, expect, Page } from "@playwright/test";
import {
  loadTestSites,
  authStatePath,
  sitecoreLogout,
} from "./sitecore-login";
import * as fs from "fs";
import * as path from "path";

const sites = loadTestSites();
const rulesScript = fs.readFileSync(
  path.join(__dirname, "rules-packages-disabled.ps1"),
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
  test.describe(`Packages disabled rules - ${site.url}`, () => {
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

    test("packaging rule conditions are restored (#1497)", async ({ page }) => {
      await setEditorAndRun(page, rulesScript);

      const terminal = page.locator(".terminal-output");
      await expect(terminal).toContainText("Done!", { timeout: 90_000 });

      const output = await terminal.innerText();
      expect(output).toContain("ConditionString=True");
      expect(output).toContain("ConditionHasAncestor=True");
      expect(output).toMatch(/OperatorName=.*not.*case.*insensitiv/i);
      expect(output).not.toMatch(/\[\[;/);
    });

    test("packaging UI follows Sitecore.Packages.Disabled (#1497)", async ({ page }) => {
      await setEditorAndRun(page, rulesScript);

      const terminal = page.locator(".terminal-output");
      await expect(terminal).toContainText("Done!", { timeout: 90_000 });

      const output = await terminal.innerText();
      expect(output).toContain("RuleUsesCondition=True");
      expect(output).toContain("PackagingMatchesFlag=True");
      expect(output).toContain("ToolboxMatchesFlag=True");
      expect(output).not.toContain("NoTrueSetting");
      expect(output).toContain("HiddenWhenTrue=False");
      expect(output).toContain("VisibleWhenAbsent=True");
      expect(output).not.toMatch(/\[\[;/);
    });
  });
}
