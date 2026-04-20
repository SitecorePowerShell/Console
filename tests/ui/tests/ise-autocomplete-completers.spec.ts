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
  // Start from a single empty tab so stale tabs from prior runs don't
  // leak state into the editor under test.
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

/**
 * Drive SPE's custom keyWordCompleter.insertMatch directly with a
 * synthetic completion item. This exercises the insertMatch branching
 * (Item / ProviderItem / ProviderContainer path-completion logic)
 * without needing a real server-populated popup - the popup's
 * `getData` / `getRow` are stubbed for the duration of the call.
 *
 * Returns the editor's text after the replacement.
 */
async function pickSyntheticCompletion(
  page: Page,
  opts: {
    line: string;
    cursorCol: number;
    meta: string;
    position: number;
    fullValue: string;
  }
): Promise<string> {
  return page.evaluate((o) => {
    const ed = (window as any).ace.edit(
      document.querySelector(".aceCodeEditor") as HTMLElement
    );
    ed.setValue(o.line, -1);
    ed.moveCursorTo(0, o.cursorCol);
    ed.focus();

    // SPE's custom keyWordCompleter is the one completer with an
    // insertMatch method and no `id` field (stock Ace completers all
    // carry ids like "snippetCompleter", "textCompleter", etc).
    const speCompleter = (ed.completers || []).find(
      (c: any) => typeof c.insertMatch === "function" && !c.id
    );
    if (!speCompleter) {
      throw new Error("SPE keyWordCompleter not found on editor.completers");
    }

    const data = {
      name: o.fullValue,
      value: o.fullValue,
      score: 1000,
      meta: o.meta,
      position: o.position,
      fullValue: o.fullValue,
    };

    // insertMatch pulls data via editor.completer.popup.getData(getRow()).
    // Stub the minimum surface and restore it afterwards so subsequent
    // real completions behave normally.
    const prevCompleter = ed.completer;
    ed.completer = {
      popup: {
        getData: () => data,
        getRow: () => 0,
      },
      completions: { filterText: "" },
    };
    try {
      speCompleter.insertMatch(ed);
      return ed.getValue();
    } finally {
      ed.completer = prevCompleter;
    }
  }, opts);
}

async function openPopupWithContent(
  page: Page,
  content: string,
  row: number,
  col: number
) {
  await page.evaluate(
    ({ c, r, co }) => {
      const ed = (window as any).ace.edit(
        document.querySelector(".aceCodeEditor") as HTMLElement
      );
      ed.setValue(c, -1);
      ed.moveCursorTo(r, co);
      ed.focus();
    },
    { c: content, r: row, co: col }
  );
  await page.keyboard.press("Control+Space");
  const popup = page.locator(".ace_editor.ace_autocomplete").first();
  await expect(popup).toBeVisible({ timeout: 15_000 });
  // Allow completer aggregation + filter + render to settle.
  await page.waitForTimeout(800);
  return popup;
}

async function getPopupData(page: Page): Promise<any[]> {
  return page.evaluate(() => {
    const ed = (window as any).ace.edit(
      document.querySelector(".aceCodeEditor") as HTMLElement
    );
    return ed?.completer?.popup?.data || [];
  });
}

async function dismissPopup(page: Page) {
  await page.keyboard.press("Escape");
}

for (const site of sites) {
  test.describe(`ISE autocomplete completers - ${site.url}`, () => {
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

    test("snippet completer is silent when prefix does not start with spe-", async ({
      page,
    }) => {
      await openPopupWithContent(page, "for", 0, 3);
      const data = await getPopupData(page);

      const snippetItems = data.filter(
        (d) => d.completerId === "snippetCompleter"
      );
      expect(snippetItems).toHaveLength(0);
      await dismissPopup(page);
    });

    test("snippet completer emits spe-* snippets when prefix starts with spe-", async ({
      page,
    }) => {
      await openPopupWithContent(page, "spe-", 0, 4);
      const data = await getPopupData(page);

      const snippetItems = data.filter(
        (d) => d.completerId === "snippetCompleter"
      );
      expect(snippetItems.length).toBeGreaterThan(0);
      // Every SPE snippet caption begins with "spe-".
      const allCaptionsArePrefixed = snippetItems.every((d) =>
        (d.caption || d.value || "").toLowerCase().startsWith("spe-")
      );
      expect(allCaptionsArePrefixed).toBeTruthy();
      await dismissPopup(page);
    });

    test("variable completer is silent when prefix does not start with $", async ({
      page,
    }) => {
      // `$testVar` is seeded in the buffer. The prefix "test" fuzzy-
      // matches "$testVar" (substring at col 1) so, if the gate were
      // broken, Ace would still include it in the popup. With the gate
      // in place, speVariableCompleter returns [] for any non-$ prefix
      // and the var is absent. The trailing `test` also gives
      // TabExpansion2 something to return (Test-Path, Test-Connection
      // etc.) so the popup opens at all.
      const content = "$testVar = 1; test";
      await openPopupWithContent(page, content, 0, content.length);
      const data = await getPopupData(page);

      const variableItems = data.filter(
        (d) => d.meta === "variable" || d.meta === "local"
      );
      expect(variableItems).toHaveLength(0);
      const hasSeededVar = data.some(
        (d) => (d.caption || d.value) === "$testVar"
      );
      expect(hasSeededVar).toBeFalsy();
      await dismissPopup(page);
    });

    test("variable completer surfaces buffer $-variables when typing $", async ({
      page,
    }) => {
      const content = "$testLocalVar = 1\n$anotherVar = 2\n$";
      await openPopupWithContent(page, content, 2, 1);
      const data = await getPopupData(page);

      const captions = data.map((d) => d.caption || d.value);
      expect(captions).toContain("$testLocalVar");
      expect(captions).toContain("$anotherVar");
      await dismissPopup(page);
    });

    test("picking an unquoted path completion consumes the token tail past the cursor", async ({
      page,
    }) => {
      // Cursor after `Get-Item master:\\content\\spe-test\\`, trailing
      // `content\\` on the right of the cursor must be removed when the
      // user picks a sibling (`master:\\content\\spe-test\\Branches`).
      const prefix = "Get-Item master:\\content\\spe-test\\";
      const line = `${prefix}content\\`;
      const result = await pickSyntheticCompletion(page, {
        line,
        cursorCol: prefix.length,
        meta: "Item",
        position: "Get-Item ".length,
        fullValue: "master:\\content\\spe-test\\Branches",
      });
      expect(result).toBe(`Get-Item master:\\content\\spe-test\\Branches`);
    });

    test("picking a quoted path completion consumes the entire quoted token including its closing quote", async ({
      page,
    }) => {
      // Cursor sits inside `'master:\\content\\...\\|User Defined' -Filter *`.
      // The replacement (`master:\\content\\Branches`, no space, so PS
      // returns it unquoted) should land unquoted and the trailing
      // `-Filter *` must survive.
      const line = `Get-Item 'master:\\content\\User Defined' -Filter *`;
      const cursorCol = "Get-Item 'master:\\content\\".length;
      const result = await pickSyntheticCompletion(page, {
        line,
        cursorCol,
        meta: "Item",
        // PowerShell's ReplacementIndex for a quoted-string expression
        // points at the opening quote itself.
        position: "Get-Item ".length,
        fullValue: "master:\\content\\Branches",
      });
      expect(result).toBe(`Get-Item master:\\content\\Branches -Filter *`);
    });

    test("picking a path completion after an already-closed quote does not wipe the rest of the line", async ({
      page,
    }) => {
      // `'master:\\content\\Branches'\\ -Filter * -AmbiguousPaths`
      // - the quote at col 34 CLOSES the earlier string, so the
      // tail (` -Filter * -AmbiguousPaths`) must survive. Previously
      // the insertMatch mistook the closing quote for an opening one
      // and walked endCol to EOL.
      const line =
        `Get-Item 'master:\\content\\Branches'\\ -Filter * -AmbiguousPaths`;
      const cursorCol = `Get-Item 'master:\\content\\Branches'\\`.length;
      const result = await pickSyntheticCompletion(page, {
        line,
        cursorCol,
        meta: "Item",
        // Trigger the firstChar=`'` branch - without the no-closing-quote
        // fallback this previously wiped the tail.
        position: "Get-Item ".length,
        fullValue: "master:\\content\\NewItem",
      });

      expect(result).toContain("-Filter * -AmbiguousPaths");
      expect(result.endsWith("-Filter * -AmbiguousPaths")).toBeTruthy();
    });

    test("autocomplete scrollbar column is compact with no gap between content and thumb", async ({
      page,
    }) => {
      // Open the popup with `spe-` so the snippet list is long enough
      // to force a scrollbar.
      const popup = await openPopupWithContent(page, "spe-", 0, 4);

      const layout = await page.evaluate(() => {
        const popup = document.querySelector(
          ".ace_editor.ace_autocomplete"
        ) as HTMLElement;
        const scroller = popup?.querySelector(".ace_scroller") as HTMLElement;
        const scrollbar = popup?.querySelector(
          ".ace_scrollbar-v"
        ) as HTMLElement;
        if (!popup || !scroller || !scrollbar) return null;
        return {
          scrollerRight: window.getComputedStyle(scroller).right,
          scrollbarWidth: window.getComputedStyle(scrollbar).width,
        };
      });

      expect(layout).not.toBeNull();
      expect(layout!.scrollerRight).toBe("10px");
      expect(layout!.scrollbarWidth).toBe("8px");

      await dismissPopup(page);
      await expect(popup).not.toBeVisible({ timeout: 5_000 });
    });
  });
}
