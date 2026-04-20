import { test, expect, Page } from "@playwright/test";
import {
  loadTestSites,
  authStatePath,
  sitecoreLogout,
  TestSite,
} from "./sitecore-login";

const sites = loadTestSites();

// A function known to ship with SPE under the Script Library Functions roots.
const KNOWN_FUNCTION = "Clear-Archive";
const BOGUS_FUNCTION = "NoSuchFunctionXyz123";

async function openIse(page: Page, site: TestSite) {
  await page.goto(
    `${site.url}/sitecore/shell/Applications/PowerShell/PowerShellIse`,
    { waitUntil: "domcontentloaded" }
  );
  // Wait for at least one tab element; ISE persists tabs across page reloads.
  await page.waitForFunction(
    () => document.querySelectorAll("[id^='Tabs_tab_']").length >= 1,
    { timeout: 20_000 }
  );
  // Switch to the first tab (indexes are 1-based) so CodeEditor1 is visible
  // regardless of persisted active-tab state from prior tests.
  await page.evaluate(() => {
    if ((window as any).spe && typeof (window as any).spe.changeTab === "function") {
      (window as any).spe.changeTab(1);
    }
  });
  await page.waitForSelector("#CodeEditor1:not([style*='display: none'])", {
    timeout: 15_000,
  });
  // Close any extra tabs left over from previous tests. Session indexes are
  // 1-based; close from the highest index down, always keeping tab 1 selected.
  const extras = await page.evaluate(() => {
    return document.querySelectorAll("[id^='Tabs_tab_']").length - 1;
  });
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
  await page.waitForFunction(
    () => {
      const el = document.querySelector("#CodeEditor1") as HTMLElement | null;
      if (!el) return false;
      const ed = (window as any).ace.edit(el);
      return !!ed && !!ed.gotoScript;
    },
    { timeout: 15_000 }
  );
}

async function setEditorText(page: Page, text: string) {
  await page.evaluate((s) => {
    const ed = (window as any).ace.edit(document.querySelector(".aceCodeEditor") as HTMLElement);
    ed.setValue(s, -1);
    ed.focus();
  }, text);
}

async function tokenRectInEditor(
  page: Page,
  row: number,
  startCol: number,
  endCol: number
): Promise<{ x: number; y: number; x2: number }> {
  return await page.evaluate(
    ([r, sc, ec]) => {
      const ed = (window as any).ace.edit(document.querySelector(".aceCodeEditor") as HTMLElement);
      const renderer = ed.renderer;
      const rect = renderer.scroller.getBoundingClientRect();
      const start = renderer.textToScreenCoordinates(r, sc);
      const end = renderer.textToScreenCoordinates(r, ec);
      // textToScreenCoordinates returns {pageX, pageY} - adjust to viewport via scroll offset
      return {
        x: start.pageX - window.scrollX,
        x2: end.pageX - window.scrollX,
        y: start.pageY - window.scrollY + renderer.lineHeight / 2,
        containerTop: rect.top,
      } as any;
    },
    [row, startCol, endCol] as const
  );
}

async function ctrlHoverOverToken(
  page: Page,
  row: number,
  startCol: number,
  endCol: number
) {
  const r = await tokenRectInEditor(page, row, startCol, endCol);
  const midX = (r.x + r.x2) / 2;
  await page.keyboard.down("Control");
  // Move to the token center so the hover handler fires with modifierDown=true.
  await page.mouse.move(midX, r.y);
  // Module uses setTimeout(120) to debounce resolve; wait slightly longer.
  await page.waitForTimeout(250);
}

async function ctrlReleaseAndAway(page: Page) {
  await page.keyboard.up("Control");
  await page.mouse.move(0, 0);
}

async function tabCount(page: Page): Promise<number> {
  return await page.locator("[id^='Tabs_tab_']").count();
}

for (const site of sites) {
  test.describe(`ISE goto-script - ${site.url}`, () => {
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

    test("Ctrl+hover on Import-Function known name renders solid underline + tooltip", async ({
      page,
    }) => {
      await setEditorText(page, `Import-Function ${KNOWN_FUNCTION}`);
      // "Import-Function " is 16 chars; the token spans cols 16..16+len
      const start = "Import-Function ".length;
      const end = start + KNOWN_FUNCTION.length;
      await ctrlHoverOverToken(page, 0, start, end);

      const tooltip = page.locator(".spe-goto-tooltip");
      await expect(tooltip).toBeVisible({ timeout: 5_000 });
      const tooltipText = await tooltip.textContent();
      expect(tooltipText || "").toMatch(/Clear-Archive/);

      // Header + body must render as distinct elements.
      const headerCount = await page
        .locator(".spe-goto-tooltip-header")
        .count();
      const bodyCount = await page
        .locator(".spe-goto-tooltip-body")
        .count();
      expect(headerCount).toBeGreaterThan(0);
      expect(bodyCount).toBeGreaterThan(0);
      const headerText = await page
        .locator(".spe-goto-tooltip-header")
        .first()
        .textContent();
      expect(headerText || "").toMatch(/Ctrl\+Click/);

      const markerKind = await page.evaluate(() => {
        const ed = (window as any).ace.edit(document.querySelector(".aceCodeEditor") as HTMLElement);
        return ed.gotoScript.markerKind;
      });
      expect(markerKind).toBe("valid");

      // The marker CSS class should be the solid underline variant.
      const hasSolid = await page
        .locator(".ace_editor .spe-goto-underline")
        .first()
        .isVisible()
        .catch(() => false);
      expect(hasSolid).toBeTruthy();

      await ctrlReleaseAndAway(page);
    });

    test("Ctrl+hover on unknown name renders dotted 'Not found' tooltip", async ({
      page,
    }) => {
      await setEditorText(page, `Import-Function ${BOGUS_FUNCTION}`);
      const start = "Import-Function ".length;
      const end = start + BOGUS_FUNCTION.length;
      await ctrlHoverOverToken(page, 0, start, end);

      const tooltip = page.locator(".spe-goto-tooltip");
      await expect(tooltip).toBeVisible({ timeout: 5_000 });
      await expect(tooltip).toHaveText("Not found");

      const markerKind = await page.evaluate(() => {
        const ed = (window as any).ace.edit(document.querySelector(".aceCodeEditor") as HTMLElement);
        return ed.gotoScript.markerKind;
      });
      expect(markerKind).toBe("missing");

      await ctrlReleaseAndAway(page);
    });

    test("ise:gotoscript Sheer handler opens the requested script", async ({
      page,
    }) => {
      // Resolve Clear-Archive via the ResolveScriptReference endpoint directly,
      // then fire ise:gotoscript with the resulting id/db and assert a new tab.
      const resolved = await page.evaluate(async () => {
        const guid = (window as any).spe?.guid || "";
        const payload = JSON.stringify({
          guid,
          kind: "function",
          target: "Clear-Archive",
          module: "",
          library: "",
        });
        const r = await fetch(
          "/sitecore modules/PowerShell/Services/PowerShellWebService.asmx/ResolveScriptReference",
          {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: payload,
          }
        );
        const txt = await r.text();
        const wrap = JSON.parse(txt);
        const inner = typeof wrap.d === "string" ? JSON.parse(wrap.d) : wrap;
        return inner;
      });

      expect(resolved?.Matches?.length || 0).toBeGreaterThan(0);
      const match = resolved.Matches[0];

      const before = await tabCount(page);
      await page.evaluate(
        ({ id, db }) => {
          (window as any).scForm.postRequest(
            "",
            "",
            "",
            `ise:gotoscript(id=${id},db=${db})`
          );
        },
        { id: match.Id, db: match.Db }
      );

      await page.waitForFunction(
        (prev) =>
          document.querySelectorAll("[id^='Tabs_tab_']").length > prev,
        before,
        { timeout: 15_000 }
      );
      expect(await tabCount(page)).toBeGreaterThan(before);
    });

    test("gotoAtCaret opens a new tab for a known function (direct API)", async ({
      page,
    }) => {
      await setEditorText(page, `Import-Function ${KNOWN_FUNCTION}`);
      const before = await tabCount(page);

      // Diagnostic: confirm _findTriggerAt returns a hit at this caret position.
      const diag = await page.evaluate((caretCol) => {
        const ed = (window as any).ace.edit(document.querySelector(".aceCodeEditor") as HTMLElement);
        ed.moveCursorTo(0, caretCol);
        const line = ed.session.getLine(0);
        const hit = ed.gotoScript._findTriggerAt(line, caretCol);
        return { hit, line };
      }, "Import-Function ".length + 2);
      expect(diag.hit).not.toBeNull();
      expect(diag.hit.target).toBe(KNOWN_FUNCTION);

      // Invoke directly and wait for either a tab change or a cache entry.
      await page.evaluate(() => {
        const ed = (window as any).ace.edit(document.querySelector(".aceCodeEditor") as HTMLElement);
        ed.focus();
        ed.gotoScript.gotoAtCaret();
      });

      await page.waitForFunction(
        (prev) =>
          document.querySelectorAll("[id^='Tabs_tab_']").length > prev,
        before,
        { timeout: 15_000 }
      );

      expect(await tabCount(page)).toBeGreaterThan(before);
    });

    test("Ctrl+F12 keyboard shortcut opens a new tab for a known function", async ({
      page,
    }) => {
      await setEditorText(page, `Import-Function ${KNOWN_FUNCTION}`);
      const before = await tabCount(page);

      await page.evaluate((caretCol) => {
        const ed = (window as any).ace.edit(document.querySelector(".aceCodeEditor") as HTMLElement);
        ed.moveCursorTo(0, caretCol);
        ed.focus();
      }, "Import-Function ".length + 2);

      // Verify the Ace command is registered before firing the shortcut.
      const cmdRegistered = await page.evaluate(() => {
        const ed = (window as any).ace.edit(document.querySelector(".aceCodeEditor") as HTMLElement);
        return !!ed.commands.commands["gotoScriptDefinition"];
      });
      expect(cmdRegistered).toBeTruthy();

      await page.keyboard.press("Control+F12");

      await page.waitForFunction(
        (prev) =>
          document.querySelectorAll("[id^='Tabs_tab_']").length > prev,
        before,
        { timeout: 15_000 }
      );

      expect(await tabCount(page)).toBeGreaterThan(before);
    });

    test("Ctrl+F12 on the same function a second time focuses existing tab, no duplicate", async ({
      page,
    }) => {
      await setEditorText(page, `Import-Function ${KNOWN_FUNCTION}`);
      const startCount = await tabCount(page);

      // First navigate
      await page.evaluate((caretCol) => {
        const ed = (window as any).ace.edit(document.querySelector(".aceCodeEditor") as HTMLElement);
        ed.moveCursorTo(0, caretCol);
        ed.focus();
      }, "Import-Function ".length + 2);
      await page.keyboard.press("Control+F12");
      await page.waitForFunction(
        (prev) =>
          document.querySelectorAll("[id^='Tabs_tab_']").length > prev,
        startCount,
        { timeout: 15_000 }
      );
      const afterFirst = await tabCount(page);

      // Switch back to tab 0 and run Ctrl+F12 again.
      await page.evaluate(() => {
        (window as any).spe.changeTab(0);
      });
      await page.waitForTimeout(250);
      await page.evaluate((caretCol) => {
        const ed = (window as any).ace.edit(document.querySelector(".aceCodeEditor") as HTMLElement);
        ed.moveCursorTo(0, caretCol);
        ed.focus();
      }, "Import-Function ".length + 2);
      await page.keyboard.press("Control+F12");

      // Give the server round-trip + handler a moment.
      await page.waitForTimeout(1500);
      const afterSecond = await tabCount(page);
      expect(afterSecond).toBe(afterFirst);
    });

  });
}
