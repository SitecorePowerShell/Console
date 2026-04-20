import { test, expect, Page } from "@playwright/test";
import {
  loadTestSites,
  authStatePath,
  sitecoreLogout,
  TestSite,
} from "./sitecore-login";

const sites = loadTestSites();

async function openIse(page: Page, site: TestSite) {
  await page.goto(
    `${site.url}/sitecore/shell/Applications/PowerShell/PowerShellIse`,
    { waitUntil: "domcontentloaded" }
  );
  await page.waitForFunction(
    () => document.querySelectorAll("[id^='Tabs_tab_']").length >= 1,
    { timeout: 20_000 }
  );
  await page.evaluate(() => {
    if ((window as any).spe && typeof (window as any).spe.changeTab === "function") {
      (window as any).spe.changeTab(1);
    }
  });
  await page.waitForSelector("#CodeEditor1:not([style*='display: none'])", {
    timeout: 15_000,
  });
  const extras = await page.evaluate(() =>
    document.querySelectorAll("[id^='Tabs_tab_']").length - 1
  );
  for (let idx = extras + 1; idx >= 2; idx--) {
    await page.evaluate((i) => {
      (window as any).scForm.postRequest(
        "",
        "",
        "",
        `ise:closetab(index=${i},modified=false,selectIndex=1)`
      );
    }, idx);
    await page
      .waitForFunction(
        (expected) =>
          document.querySelectorAll("[id^='Tabs_tab_']").length === expected,
        idx - 1,
        { timeout: 10_000 }
      )
      .catch(() => {});
  }
}

for (const site of sites) {
  test.describe(`ISE Invoke-Script -Path autocomplete - ${site.url}`, () => {
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
      await openIse(page, site);
    });

    test("popup lists single-quoted backslash paths and auto-sizes to content", async ({
      page,
    }) => {
      await page.evaluate(() => {
        const ed = (window as any).ace.edit(
          document.querySelector(".aceCodeEditor") as HTMLElement
        );
        ed.setValue("Invoke-Script -Path ", -1);
        const line = ed.session.getLine(0);
        ed.moveCursorTo(0, line.length);
        ed.focus();
      });

      await page.keyboard.press("Control+Space");

      const popup = page.locator(".ace_editor.ace_autocomplete").first();
      await expect(popup).toBeVisible({ timeout: 15_000 });
      // Give setData + width patch + renderer resize a moment to settle.
      await page.waitForTimeout(800);

      const width = await popup.evaluate(
        (el) => el.getBoundingClientRect().width
      );
      // Default Ace width is 300px; auto-sizing should push it wider for
      // realistic Script Library paths.
      expect(width).toBeGreaterThan(320);

      const anyRowHasBackslashQuoted = await page.evaluate(() => {
        const popupData =
          (window as any).ace
            .edit(document.querySelector(".aceCodeEditor") as HTMLElement)
            ?.completer?.popup?.data || [];
        return popupData.some(
          (d: any) =>
            typeof (d.caption || d.value) === "string" &&
            /^'.+\\.+'$/.test(d.caption || d.value)
        );
      });
      expect(anyRowHasBackslashQuoted).toBeTruthy();
    });
  });
}
