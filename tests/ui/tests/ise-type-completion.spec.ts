import { test, expect, Page } from "@playwright/test";
import {
  loadTestSites,
  authStatePath,
  sitecoreLogout,
  TestSite,
} from "./sitecore-login";

// #1397: on XM Cloud PowerShell's own type completion succeeds, so TabExpansion2
// reports the replacement index after the opening bracket. SPE used to take that
// as "not a type token" and fall through to raw TabExpansion2 output, which the
// ISE cannot insert (it ended up writing "[]").

const sites = loadTestSites();
const TYPED = "[Sitecore.Data.Items.Ite]";
const CURSOR_COL = TYPED.length - 1; // just before the closing bracket

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
  const extras = await page.evaluate(
    () => document.querySelectorAll("[id^='Tabs_tab_']").length - 1
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

async function setEditor(page: Page, text: string, col?: number) {
  await page.evaluate(
    ({ t, c }) => {
      const ed = (window as any).ace.edit(
        document.querySelector(".aceCodeEditor") as HTMLElement
      );
      ed.setValue(t, -1);
      if (typeof c === "number") {
        ed.moveCursorTo(0, c);
      }
      ed.focus();
    },
    { t: text, c: col }
  );
}

for (const site of sites) {
  test.describe(`ISE type-name completion (#1397) - ${site.url}`, () => {
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

    test("picking a type from the popup completes the bracketed name", async ({
      page,
    }) => {
      await setEditor(page, TYPED, CURSOR_COL);
      await page.keyboard.press("Control+Space");

      const popup = page.locator(".ace_editor.ace_autocomplete").first();
      await expect(popup).toBeVisible({ timeout: 15_000 });
      await page.waitForTimeout(800);

      const line = await page.evaluate(() => {
        const ed = (window as any).ace.edit(
          document.querySelector(".aceCodeEditor") as HTMLElement
        );
        const rows: any[] = ed.completer?.popup?.data || [];
        const idx = rows.findIndex(
          (d) => d.meta === "Type" && d.fullValue === "Sitecore.Data.Items.Item"
        );
        if (idx < 0) {
          throw new Error(
            "No Type row for Sitecore.Data.Items.Item; rows=" +
              JSON.stringify(rows.slice(0, 5).map((d) => [d.meta, d.name, d.fullValue]))
          );
        }
        ed.completer.popup.setRow(idx);
        ed.completer.insertMatch();
        return ed.session.getLine(0);
      });

      expect(line).toBe("[Sitecore.Data.Items.Item]");
    });
  });
}
