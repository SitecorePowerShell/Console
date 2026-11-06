import { test, expect, Page } from "@playwright/test";
import {
  loadTestSites,
  authStatePath,
  refreshSitecoreSession,
} from "./sitecore-login";

const sites = loadTestSites();

const LONG_LINE =
  "Get-Item -Path master:\\content\\Home -Language en-US | Select-Object -Property Name, Id, Paths, Fields";

// Average cost of one ArrowLeft redraw, driven through the terminal's own keydown handler.
// Position is read in the same tick: the Console later moves the cursor back to the end on its own.
function arrowLeft(page: Page, selector: string): Promise<{ ms: number; position: number }> {
  return page.evaluate(
    ({ sel, line }) => {
      const $ = (window as any).jQuery;
      const term = $(sel).terminal();
      term.set_command(line);
      term.set_position(line.length);
      const target = document.activeElement;
      const t0 = performance.now();
      for (let i = 0; i < 40; i++) {
        $(target).trigger($.Event("keydown", { which: 37, keyCode: 37, key: "ArrowLeft" }));
      }
      return { ms: (performance.now() - t0) / 40, position: term.get_position() };
    },
    { sel: selector, line: LONG_LINE }
  );
}

for (const site of sites) {
  test.describe(`Terminal cursor speed - ${site.url}`, () => {
    test.use({ storageState: authStatePath(site) });

    test.afterAll(async ({ browser }) => {
      const context = await browser.newContext({
        ignoreHTTPSErrors: true,
        storageState: authStatePath(site),
      });
      const page = await context.newPage();
      await refreshSitecoreSession(page, site);
      await page.close();
      await context.close();
    });

    test.beforeEach(async ({ page }) => {
      await page.goto(`${site.url}/sitecore/shell/applications/powershell/powershell-console`, {
        waitUntil: "domcontentloaded",
      });
      await page.waitForFunction(
        () => {
          const t = (window as any).jQuery("#terminal").terminal();
          return t && !t.paused();
        },
        null,
        { timeout: 60_000 }
      );
      await page.locator("#terminal .cmd").click();
    });

    test("char_substrings matches per-character substring", async ({ page }) => {
      const mismatches = await page.evaluate(() => {
        const $ = (window as any).jQuery;
        const samples = [
          "Get-Item",
          "[[;yellow;]Get-Item] [[;magenta;]-Path] master:\\content\\Home",
          "[[;lightgreen;]$x] = 'a &amp; b' &nbsp; [[;yellow;]Write-Host]",
          "[[b;red;]bold] plain [[;;]x] tail\\",
          "escaped \\] bracket [[;yellow;]in \\] fmt]",
          "emoji 😀 and combining é done",
          "tab\there",
          "",
        ];
        const bad: string[] = [];
        for (const s of samples) {
          const len = $.terminal.length(s);
          const expected: string[] = [];
          for (let i = 0; i < len; i++) expected.push($.terminal.substring(s, i, i + 1));
          const actual = $.terminal.char_substrings(s);
          if (JSON.stringify(actual) !== JSON.stringify(expected)) {
            bad.push(JSON.stringify({ s, expected, actual }));
          }
        }
        return bad;
      });
      expect(mismatches).toEqual([]);
    });

    test("split_characters keeps emoji, keycaps and combining marks whole", async ({ page }) => {
      const chars = await page.evaluate(() =>
        (window as any).jQuery.terminal.split_characters("aé😀 1️⃣x©")
      );
      expect(chars).toEqual(["a", "é", "😀", " ", "1️⃣", "x", "©"]);
    });

    test("arrow keys move the cursor quickly on a long line", async ({ page }) => {
      const result = await arrowLeft(page, "#terminal");
      expect(result.position).toBe(LONG_LINE.length - 40);
      expect(result.ms).toBeLessThan(10);
    });

    // Auto-repeat of a held key: keyboard.down on a key that is already down sends repeat keydowns.
    async function holdKey(page: Page, key: string, repeats: number) {
      for (let i = 0; i < repeats; i++) {
        await page.keyboard.down(key);
        await page.waitForTimeout(33);
      }
      await page.keyboard.up(key);
    }

    test("holding an arrow moves the cursor at the key repeat rate", async ({ page }) => {
      await page.evaluate((line) => {
        const t = (window as any).jQuery("#terminal").terminal();
        t.set_command(line);
      }, LONG_LINE);
      await page.keyboard.press("End");
      await holdKey(page, "ArrowLeft", 45);
      const moved = await page.evaluate(
        (len) => len - (window as any).jQuery("#terminal").terminal().get_position(),
        LONG_LINE.length
      );
      expect(moved).toBeGreaterThanOrEqual(35);
    });

    test("holding backspace still deletes words slowly", async ({ page }) => {
      const words = Array.from({ length: 40 }, (_, i) => "word" + i).join(" ");
      await page.evaluate((line) => (window as any).jQuery("#terminal").terminal().set_command(line), words);
      await page.keyboard.press("End");
      await holdKey(page, "Backspace", 45);
      const left = await page.evaluate(() =>
        (window as any).jQuery("#terminal").terminal().get_command().split(" ").filter(Boolean).length
      );
      expect(left).toBeGreaterThanOrEqual(25);
    });

    test("cursor stays where real arrow presses leave it", async ({ page }) => {
      await page.keyboard.type("Get-Item -Path master:\\content\\Home -Language en-US", { delay: 5 });
      for (let i = 0; i < 10; i++) await page.keyboard.press("ArrowLeft");
      await page.waitForTimeout(1500);
      const state = await page.evaluate(() => {
        const t = (window as any).jQuery("#terminal").terminal();
        return { position: t.get_position(), length: t.get_command().length };
      });
      expect(state.position).toBe(state.length - 10);
    });
  });
}
