import { test, expect, Page } from "@playwright/test";
import {
  loadTestSites,
  authStatePath,
  sitecoreLogout,
  TestSite,
} from "./sitecore-login";

const sites = loadTestSites();

const EXEC_MENU_BUTTON = "B0F4A0BC54AC8B47EBACCBE09F08D5C85C_menu_button";
// The split-button renders its menu as table rows with class
// ".scGalleryMenuItem" directly in the top document (not inside an iframe,
// unlike the gallery-style dropdowns in the Context ribbon).
const MENU_ITEM_SELECTOR = ".scGalleryMenuItem";

async function openIse(page: Page, site: TestSite) {
  await page.goto(
    `${site.url}/sitecore/shell/Applications/PowerShell/PowerShellIse`,
    { waitUntil: "domcontentloaded" }
  );
  await page.waitForSelector(".ace_editor", { timeout: 15_000 });
}

async function setEditor(page: Page, script: string) {
  await page.evaluate((s) => {
    const editor = (window as any).ace.edit(
      document.querySelector(".ace_editor") as HTMLElement
    );
    editor.setValue(s, -1);
  }, script);
}

async function openExecuteMenu(page: Page) {
  await page.evaluate((id) => {
    document.getElementById(id)?.click();
  }, EXEC_MENU_BUTTON);
}

async function waitForExecuteMenu(page: Page) {
  // Several split-buttons on the ribbon (New, Open, Execute, ...) each own a
  // set of scGalleryMenuItem rows that stay in the DOM but hidden until their
  // button is clicked. Wait for the specific Execute item text to be visibly
  // rendered rather than for ".scGalleryMenuItem" as a class - Playwright's
  // default state:visible wait otherwise picks the first hidden sibling from
  // the New menu and times out.
  await page.waitForFunction(() => {
    const rows = Array.from(
      document.querySelectorAll<HTMLElement>(".scGalleryMenuItem")
    );
    return rows.some(
      (r) =>
        r.textContent?.trim() === "Execute in the runner dialog" &&
        r.offsetParent !== null
    );
  }, { timeout: 10_000 });
}

async function clickMenuItem(page: Page, label: string) {
  await page.evaluate(
    ([selector, l]) => {
      const items = Array.from(
        document.querySelectorAll<HTMLElement>(selector)
      );
      const target = items.find((i) => i.textContent?.trim() === l);
      target?.click();
    },
    [MENU_ITEM_SELECTOR, label] as const
  );
}

// Navigate away so any open Sheer modals and any paused ise:execute scripts
// are torn down before the next test starts. Going to about:blank drops the
// ISE page entirely, which is simpler and more reliable than trying to close
// nested modal iframes one by one. The per-test sessionId trick below is what
// stops a paused Show-Result from locking the shared "ISE editing session"
// that other spec files (ise.spec.ts) run against.
async function resetIseSession(page: Page) {
  try {
    await page.goto("about:blank", { waitUntil: "domcontentloaded", timeout: 5_000 });
  } catch (_) {
    /* best-effort cleanup */
  }
}

// Rendered jquery.terminal output uses \u00A0 (nbsp) between glyph runs to
// preserve spacing. Normalise to regular spaces before substring checks so
// the assertion matches "runner test" in either "runner test" (regular
// space) or "runner\u00A0test" (nbsp).
function normalize(s: string): string {
  return s.replace(/\u00A0/g, " ");
}

for (const site of sites) {
  test.describe(`ISE result viewer - ${site.url}`, () => {
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

    test.afterEach(async ({ page }) => {
      await resetIseSession(page);
    });

    test("Execute dropdown exposes \"Execute in the runner dialog\"", async ({
      page,
    }) => {
      await openExecuteMenu(page);
      await waitForExecuteMenu(page);
      const labels = await page.evaluate((selector) => {
        return Array.from(
          document.querySelectorAll<HTMLElement>(selector)
        )
          .map((el) => el.textContent?.trim() ?? "")
          .filter(Boolean);
      }, MENU_ITEM_SELECTOR);
      expect(labels).toContain("Execute in the runner dialog");
    });

    test("Runner's \"View Script Results\" link renders a terminal, not raw jsterm markup", async ({
      page,
    }) => {
      // A tiny script so the runner reaches Done quickly. -ForegroundColor
      // Yellow gives us a span we can assert jquery.terminal emitted colour
      // markup for, instead of leaving raw [[;...]...] in the output.
      await setEditor(page, `Write-Host "runner test" -ForegroundColor Yellow`);
      await openExecuteMenu(page);
      await waitForExecuteMenu(page);
      await clickMenuItem(page, "Execute in the runner dialog");

      // Wait for the runner to finish and reveal the View Results link.
      await page.waitForFunction(
        () => {
          const modal = document.getElementById(
            "jqueryModalDialogsFrame"
          ) as HTMLIFrameElement | null;
          const runner = modal?.contentDocument?.getElementById(
            "scContentIframeId0"
          ) as HTMLIFrameElement | null;
          const link = (runner?.contentDocument?.getElementById(
            "ViewButton"
          ) ||
            runner?.contentDocument?.getElementById(
              "ViewErrorsButton"
            )) as HTMLAnchorElement | null;
          if (!link || !runner) return false;
          const cs = runner.contentWindow!.getComputedStyle(link);
          return cs.display !== "none";
        },
        { timeout: 30_000 }
      );

      await page.evaluate(() => {
        const modal = document.getElementById(
          "jqueryModalDialogsFrame"
        ) as HTMLIFrameElement;
        const runner = modal.contentDocument!.getElementById(
          "scContentIframeId0"
        ) as HTMLIFrameElement;
        const link = (runner.contentDocument!.getElementById("ViewButton") ||
          runner.contentDocument!.getElementById(
            "ViewErrorsButton"
          )) as HTMLAnchorElement;
        link.click();
      });

      // The viewer opens as the next modal in the stack.
      await page.waitForFunction(
        () => {
          const modal = document.getElementById(
            "jqueryModalDialogsFrame"
          ) as HTMLIFrameElement | null;
          const viewer = modal?.contentDocument?.getElementById(
            "scContentIframeId1"
          ) as HTMLIFrameElement | null;
          return !!viewer?.contentDocument?.querySelector(
            "#resultTerminal .terminal-output"
          );
        },
        { timeout: 10_000 }
      );

      const state = await page.evaluate(() => {
        const modal = document.getElementById(
          "jqueryModalDialogsFrame"
        ) as HTMLIFrameElement;
        const viewer = modal.contentDocument!.getElementById(
          "scContentIframeId1"
        ) as HTMLIFrameElement;
        const doc = viewer.contentDocument!;
        const terminal = doc.querySelector("#resultTerminal") as HTMLElement;
        return {
          hasTerminal: !!terminal,
          innerText: terminal?.innerText ?? "",
          spanCount: doc.querySelectorAll("#resultTerminal .terminal-output span")
            .length,
        };
      });

      expect(state.hasTerminal).toBe(true);
      expect(normalize(state.innerText)).toContain("runner test");
      // Raw jquery.terminal format strings must not bleed into the rendered
      // output; if they do the echo path is broken.
      expect(state.innerText).not.toMatch(/\[\[;/);
      expect(state.spanCount).toBeGreaterThan(0);
    });

    test("Show-Result -Text opens the viewer with the terminal, honoring the session background", async ({
      page,
    }) => {
      // Show-Result -Text pipes session.Output.ToJsTerminalString() through
      // the same viewer the Runner uses. Verify the content renders through
      // the terminal (no raw markup) and that the background came from the
      // session's default (DarkBlue -> #012456) rather than the jquery.terminal
      // default black.
      //
      // Use a test-scoped sessionId so that if the Show-Result modal is still
      // paused when the test tears down, the leftover script sits in a
      // dedicated session instead of locking the shared "ISE editing session"
      // that other spec files (ise.spec.ts, ise-policy.spec.ts) run against.
      const sessionId = `ise-result-viewer-${Date.now()}`;
      await page.evaluate((id) => {
        (window as any).scForm.postRequest(
          "",
          "",
          "",
          `ise:setsessionid(id=${id})`
        );
      }, sessionId);

      await setEditor(
        page,
        `Write-Host "show result" -ForegroundColor Magenta\nShow-Result -Text`
      );
      await page.keyboard.press("Control+e");

      await page.waitForFunction(
        () => {
          const modal = document.getElementById(
            "jqueryModalDialogsFrame"
          ) as HTMLIFrameElement | null;
          const viewer = modal?.contentDocument?.querySelector(
            'iframe[src*="PowerShellResultViewerText"]'
          ) as HTMLIFrameElement | null;
          return !!viewer?.contentDocument?.querySelector(
            "#resultTerminal .terminal-output"
          );
        },
        { timeout: 60_000 }
      );

      const state = await page.evaluate(() => {
        const modal = document.getElementById(
          "jqueryModalDialogsFrame"
        ) as HTMLIFrameElement;
        const viewer = modal.contentDocument!.querySelector(
          'iframe[src*="PowerShellResultViewerText"]'
        ) as HTMLIFrameElement;
        const doc = viewer.contentDocument!;
        const terminal = doc.querySelector("#resultTerminal") as HTMLElement;
        const host = terminal?.parentElement as HTMLElement;
        const win = viewer.contentWindow!;
        return {
          innerText: terminal?.innerText ?? "",
          spanCount: doc.querySelectorAll("#resultTerminal .terminal-output span")
            .length,
          // The host Border (#TerminalHost) takes its colours from the session
          // settings; the terminal inherits them via CSS.
          hostBg: host ? win.getComputedStyle(host).backgroundColor : "",
        };
      });

      expect(normalize(state.innerText)).toContain("show result");
      expect(state.innerText).not.toMatch(/\[\[;/);
      expect(state.spanCount).toBeGreaterThan(0);
      // DarkBlue -> #012456 -> rgb(1, 36, 86). Jquery.terminal's default is
      // black; seeing rgb(1, 36, 86) proves the session-colour path is wired.
      expect(state.hostBg).toContain("1, 36, 86");
    });
  });
}
