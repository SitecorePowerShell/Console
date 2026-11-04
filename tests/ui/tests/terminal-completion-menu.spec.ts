import { test, expect, Page } from "@playwright/test";
import {
  loadTestSites,
  authStatePath,
  refreshSitecoreSession,
} from "./sitecore-login";
import * as fs from "fs";
import * as path from "path";

const sites = loadTestSites();
const debugScript = fs.readFileSync(
  path.join(__dirname, "terminal-completion-menu-debug.ps1"),
  "utf-8"
);

function countRequests(page: Page, endpoint: string): () => number {
  let count = 0;
  page.on("request", (r) => {
    if (r.url().includes(`/${endpoint}`)) count++;
  });
  return () => count;
}

type Entry = { kind: string; text: string; position: number; value: string };

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

async function openConsole(page: Page, url: string) {
  await page.goto(
    `${url}/sitecore/shell/applications/powershell/powershell-console`,
    { waitUntil: "domcontentloaded" }
  );
  await waitForConsoleReady(page);
}

const consoleTerm = "#terminal";
const iseTerm = "#ScriptResultCode";

// ISE restores open tabs server-side; start every test from a single tab.
async function openIse(page: Page, url: string) {
  await page.goto(`${url}/sitecore/shell/Applications/PowerShell/PowerShellIse`, {
    waitUntil: "domcontentloaded",
  });
  await page.waitForFunction(
    () => document.querySelectorAll("[id^='Tabs_tab_']").length >= 1,
    { timeout: 20_000 }
  );
  await page.waitForSelector(".ace_editor", { timeout: 15_000 });
  const extras = await page.evaluate(
    () => document.querySelectorAll("[id^='Tabs_tab_']").length - 1
  );
  for (let idx = extras + 1; idx >= 2; idx--) {
    await page.evaluate((i) => {
      (window as any).scForm.postRequest("", "", "", `ise:closetab(index=${i},modified=false,selectIndex=1)`);
    }, idx);
    await page
      .waitForFunction(
        (expected) => document.querySelectorAll("[id^='Tabs_tab_']").length === expected,
        idx - 1,
        { timeout: 10_000 }
      )
      .catch(() => {});
  }
  // The ISE moves focus to the editor late (page load, closetab's selectIndex); click until the terminal keeps it.
  const terminalFocused = () =>
    page.evaluate((sel) => {
      const el = document.activeElement;
      return !!el && !!el.closest(sel);
    }, iseTerm);
  for (let attempt = 0; attempt < 10; attempt++) {
    await page.locator(`${iseTerm} .cmd`).click();
    await page.waitForTimeout(500);
    if (await terminalFocused()) {
      await page.waitForTimeout(500);
      if (await terminalFocused()) return;
    }
  }
  throw new Error("ISE terminal never kept keyboard focus");
}

const menu = (page: Page) => page.locator(".spe-completion-menu");
const menuRows = (page: Page) => page.locator(".spe-completion-menu li:not(.info)");

// Colour of an unselected row's text; callers move the selection off the row first.
function rowColor(page: Page, index: number): Promise<string> {
  return page.evaluate(
    (i) => getComputedStyle(document.querySelectorAll(".spe-completion-menu li:not(.info) .text")[i]).color,
    index
  );
}

function getCommand(page: Page, selector: string): Promise<string> {
  return page.evaluate((sel) => (window as any).jQuery(sel).terminal().get_command(), selector);
}

// Echo of the typed command counts once, so more than one hit means the command ran.
function countOf(text: string, needle: string): number {
  return text.replace(/ /g, " ").split(needle).length - 1;
}

const cmd = (value: string, position = 0): Entry => ({
  kind: "Command",
  text: value,
  position,
  value,
});

for (const site of sites) {
  test.describe(`Terminal completion menu - ${site.url}`, () => {
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

    test.describe("helpers", () => {
      let page: Page;

      test.beforeAll(async ({ browser }) => {
        const context = await browser.newContext({
          ignoreHTTPSErrors: true,
          storageState: authStatePath(site),
        });
        page = await context.newPage();
        await openConsole(page, site.url);
      });

      test.afterAll(async () => {
        await page.context().close();
      });

      const parse = (raw: string) =>
        page.evaluate((r) => (window as any).spe.terminalCompletion.parseHints(r), raw);
      const apply = (command: string, cursor: number, entry: Entry) =>
        page.evaluate(
          ({ c, p, e }) => (window as any).spe.terminalCompletion.applyCompletion(c, p, e),
          { c: command, p: cursor, e: entry }
        );
      const filter = (entries: Entry[], command: string, cursor: number) =>
        page.evaluate(
          ({ l, c, p }) => (window as any).spe.terminalCompletion.filterEntries(l, c, p),
          { l: entries, c: command, p: cursor }
        );

      test("parseHints handles empty and non-elevated replies", async () => {
        expect(await parse('""')).toEqual({ entries: [], signatures: [] });
        expect(await parse("")).toEqual({ entries: [], signatures: [] });
      });

      test("parseHints reads typed rows", async () => {
        const parsed = await parse(
          JSON.stringify(["Command|Get-Item|0", "Parameter|-Path|9"])
        );
        expect(parsed.entries).toEqual([
          { kind: "Command", text: "Get-Item", position: 0, value: "Get-Item" },
          { kind: "Parameter", text: "-Path", position: 9, value: "-Path" },
        ]);
        expect(parsed.signatures).toEqual([]);
      });

      test("parseHints collects signatures", async () => {
        const parsed = await parse(
          JSON.stringify([
            "Signature|Format(string)|14|static string Format(string format, Params object[] args)|14",
          ])
        );
        expect(parsed.entries).toEqual([]);
        expect(parsed.signatures).toEqual([
          "static string Format(string format, Params object[] args)",
        ]);
      });

      test("applyCompletion replaces a command token", async () => {
        expect(await apply("Get-Ite", 7, cmd("Get-Item"))).toEqual({
          command: "Get-Item",
          cursor: 8,
        });
      });

      test("applyCompletion completes a type after the bracket", async () => {
        const entry = {
          kind: "Type",
          text: "string (Accelerator -> System.String)",
          position: 0,
          value: "string",
        };
        expect(await apply("[Strin", 6, entry)).toEqual({
          command: "[string",
          cursor: 7,
        });
      });

      test("applyCompletion replaces a quoted path through its closing quote", async () => {
        const q = "Get-Item 'master:\\content\\Ho' -Recurse";
        const entry = {
          kind: "Item",
          text: "Home",
          position: 9,
          value: "master:\\content\\Home",
        };
        expect(await apply(q, 28, entry)).toEqual({
          command: "Get-Item 'master:\\content\\Home' -Recurse",
          cursor: 31,
        });
      });

      test("applyCompletion replaces an unquoted path to the end of the token", async () => {
        const u = "Get-Item master:\\content\\Ho -Recurse";
        const entry = {
          kind: "Item",
          text: "Home",
          position: 9,
          value: "master:\\content\\Home",
        };
        expect(await apply(u, 27, entry)).toEqual({
          command: "Get-Item master:\\content\\Home -Recurse",
          cursor: 29,
        });
      });

      test("filterEntries narrows case-insensitively", async () => {
        const result: Entry[] = await filter(
          [cmd("Get-Item"), cmd("Get-ItemField"), cmd("Get-Location")],
          "get-itemf",
          9
        );
        expect(result.map((e) => e.value)).toEqual(["Get-ItemField"]);
      });

      test("filterEntries keeps a type matched by its short name", async () => {
        const entry = {
          kind: "Type",
          text: "ArrayList (System.Collections)",
          position: 0,
          value: "System.Collections.ArrayList",
        };
        expect((await filter([entry], "[ArrayLi", 8)).length).toBe(1);
      });

      test("filterEntries keeps a relative path matched by its leaf", async () => {
        const entry = { kind: "ProviderContainer", text: "content", position: 3, value: ".\\content" };
        expect((await filter([entry], "cd cont", 7)).length).toBe(1);
      });

      test("applyCompletion replaces the rest of a command word after the cursor", async () => {
        expect(await apply("Get-Item", 6, cmd("Get-Item"))).toEqual({ command: "Get-Item", cursor: 8 });
      });

      test("applyCompletion replaces the rest of a parameter after the cursor", async () => {
        const entry = { kind: "Parameter", text: "-Path", position: 9, value: "-Path" };
        expect(await apply("Get-Item -Path x", 12, entry)).toEqual({ command: "Get-Item -Path x", cursor: 14 });
      });

      test("applyCompletion replaces the rest of a type name after the cursor", async () => {
        const entry = { kind: "Type", text: "string (Accelerator -> System.String)", position: 0, value: "string" };
        expect(await apply("[Strin]", 3, entry)).toEqual({ command: "[string]", cursor: 7 });
      });

      test("applyCompletion ignores an earlier bracket for a bare type name", async () => {
        const line = "$a = [int]1; New-Object -TypeName System.Coll";
        const entry = {
          kind: "Type",
          text: "ArrayList (System.Collections)",
          position: 34,
          value: "System.Collections.ArrayList",
        };
        expect(await apply(line, 45, entry)).toEqual({
          command: "$a = [int]1; New-Object -TypeName System.Collections.ArrayList",
          cursor: 62,
        });
      });
    });

    test.describe("Console", () => {
      const term = consoleTerm;

      test.beforeEach(async ({ page }) => {
        await openConsole(page, site.url);
        await page.locator("#terminal .cmd").click();
      });

      test("tab opens menu and enter inserts the selected entry", async ({ page }) => {
        await page.keyboard.type("Get-Ite", { delay: 20 });
        await page.keyboard.press("Tab");
        const rows = menuRows(page);
        await expect.poll(() => rows.count(), { timeout: 15_000 }).toBeGreaterThan(1);
        await expect(rows.first().locator(".text")).toHaveText(/^Get-Item/);
        const second = await rows.nth(1).locator(".text").innerText();
        await page.keyboard.press("ArrowDown");
        await expect(rows.nth(1)).toHaveClass(/selected/);
        await page.keyboard.press("Enter");
        await expect(menu(page)).toBeHidden();
        expect(await getCommand(page, term)).toBe(second);
      });

      test("esc closes the menu and keeps the command", async ({ page }) => {
        await page.keyboard.type("Get-Ite", { delay: 20 });
        await page.keyboard.press("Tab");
        await expect(menu(page)).toBeVisible({ timeout: 15_000 });
        await page.keyboard.press("Escape");
        await expect(menu(page)).toBeHidden();
        expect(await getCommand(page, term)).toBe("Get-Ite");
      });

      test("ctrl+space opens the menu", async ({ page }) => {
        await page.keyboard.type("Get-Ite", { delay: 20 });
        await page.keyboard.press("Control+Space");
        await expect.poll(() => menuRows(page).count(), { timeout: 15_000 }).toBeGreaterThan(1);
        expect(await getCommand(page, term)).toBe("Get-Ite");
      });

      test("single match inserts without a menu", async ({ page }) => {
        await page.keyboard.type("Get-ItemPropertyValu", { delay: 20 });
        await page.keyboard.press("Tab");
        await expect
          .poll(() => getCommand(page, term), { timeout: 15_000 })
          .toBe("Get-ItemPropertyValue");
        await expect(menu(page)).toBeHidden();
      });

      test("enter runs the command when the menu is closed", async ({ page }) => {
        await page.keyboard.type("Write-Host 'zz-menu-run'", { delay: 20 });
        await page.keyboard.press("Enter");
        await expect
          .poll(async () => countOf(await page.locator(".terminal-output").innerText(), "zz-menu-run"), {
            timeout: 30_000,
          })
          .toBeGreaterThan(1);
      });

      // Pushed directly: console.js's own continuation check cannot match a typed trailing backtick.
      test("menu works under a pushed nested interpreter", async ({ page }) => {
        await page.evaluate((sel) => {
          (window as any).jQuery(sel).terminal().push(function () {}, { prompt: ">>", name: "nested" });
        }, term);
        await page.keyboard.type("Get-Ite", { delay: 20 });
        await page.keyboard.press("Tab");
        await expect(menu(page)).toBeVisible({ timeout: 15_000 });
        await page.keyboard.press("Escape");
        await expect(menu(page)).toBeHidden();
        expect(await getCommand(page, term)).toBe("Get-Ite");
      });

      test("live on opens after a pause and narrows without refetching", async ({ page }) => {
        const requests = countRequests(page, "CompleteCommand");
        await page.evaluate(() => (window as any).spe.setOptions({ liveAutocompletion: true }));
        await page.keyboard.type("Get-Ite", { delay: 20 });
        await page.waitForTimeout(600);
        await expect(menu(page)).toBeVisible({ timeout: 15_000 });
        expect(requests()).toBe(1);
        await page.keyboard.type("mP", { delay: 20 });
        const texts = await menuRows(page).locator(".text").allInnerTexts();
        expect(texts.length).toBeGreaterThan(0);
        for (const t of texts) expect(t.startsWith("Get-ItemP")).toBe(true);
        expect(requests()).toBe(1);
      });

      test("live off never opens while typing", async ({ page }) => {
        const requests = countRequests(page, "CompleteCommand");
        await page.keyboard.type("Get-Ite", { delay: 20 });
        await page.waitForTimeout(800);
        await expect(menu(page)).toBeHidden();
        expect(requests()).toBe(0);
      });

      test("enter before the pause runs the command and opens nothing", async ({ page }) => {
        await page.evaluate(() => (window as any).spe.setOptions({ liveAutocompletion: true }));
        await page.keyboard.type("Write-Host 'zz-live'", { delay: 20 });
        await page.keyboard.press("Enter");
        await page.waitForTimeout(800);
        await expect(menu(page)).toBeHidden();
        await expect
          .poll(async () => countOf(await page.locator(".terminal-output").innerText(), "zz-live"), {
            timeout: 30_000,
          })
          .toBeGreaterThan(1);
        await page.waitForTimeout(800);
        await expect(menu(page)).toBeHidden();
      });

      test("slow reply shows a loading row until results arrive", async ({ page }) => {
        await page.route("**/CompleteCommand", async (route) => {
          await new Promise((r) => setTimeout(r, 1500));
          await route.continue();
        });
        await page.keyboard.type("Get-Ite", { delay: 20 });
        await page.keyboard.press("Tab");
        await expect(page.locator(".spe-completion-menu li.loading")).toBeVisible({ timeout: 1_000 });
        await expect.poll(() => menuRows(page).count(), { timeout: 15_000 }).toBeGreaterThan(1);
        await expect(page.locator(".spe-completion-menu li.loading")).toHaveCount(0);
      });

      test("menu scrollbar matches the terminal scrollbar", async ({ page }) => {
        await page.keyboard.type("Get-Ite", { delay: 20 });
        await page.keyboard.press("Tab");
        await expect(menu(page)).toBeVisible({ timeout: 15_000 });
        const colors = await page.evaluate(() => ({
          menu: getComputedStyle(document.querySelector(".spe-completion-menu")!).scrollbarColor,
          terminal: getComputedStyle(document.querySelector("#terminal")!).scrollbarColor,
        }));
        expect(colors.menu).toBe(colors.terminal);
      });

      test("a reply for a line the user kept typing is dropped", async ({ page }) => {
        await page.route("**/CompleteCommand", async (route) => {
          await new Promise((r) => setTimeout(r, 1500));
          await route.continue();
        });
        await page.keyboard.type("Get-Ite", { delay: 20 });
        await page.keyboard.press("Tab");
        await page.keyboard.type("m -Path x", { delay: 20 });
        await page.waitForTimeout(2500);
        await expect(menuRows(page)).toHaveCount(0);
        expect(await getCommand(page, term)).toBe("Get-Item -Path x");
      });

      test("menu closes when the terminal pauses or loses focus", async ({ page }) => {
        await page.keyboard.type("Get-Ite", { delay: 20 });
        await page.keyboard.press("Tab");
        await expect(menu(page)).toBeVisible({ timeout: 15_000 });
        await page.evaluate((sel) => (window as any).jQuery(sel).terminal().pause(), term);
        await expect(menu(page)).toBeHidden();
        await page.evaluate((sel) => (window as any).jQuery(sel).terminal().resume(), term);
        await page.locator(`${term} .cmd`).click();
        await page.keyboard.press("Tab");
        await expect(menu(page)).toBeVisible({ timeout: 15_000 });
        await page.evaluate((sel) => (window as any).jQuery(sel).terminal().disable(), term);
        await expect(menu(page)).toBeHidden();
      });

      test("menu frame uses the scrollbar thumb colour", async ({ page }) => {
        await page.keyboard.type("Get-Ite", { delay: 20 });
        await page.keyboard.press("Tab");
        await expect(menu(page)).toBeVisible({ timeout: 15_000 });
        const border = await page.evaluate(
          () => getComputedStyle(document.querySelector(".spe-completion-menu")!).borderTopColor
        );
        expect(border).toBe("rgba(255, 255, 255, 0.3)");
      });

      test("non-elevated session says so and asks for elevation", async ({ page }) => {
        await page.route("**/CompleteCommand", (route) =>
          route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ d: "" }) })
        );
        await page.evaluate(() => {
          (window as any).__elevations = 0;
          (window as any).spe.elevateSession = () => (window as any).__elevations++;
        });
        await page.keyboard.type("Get-Ite", { delay: 20 });
        await page.keyboard.press("Tab");
        await expect(page.locator(".spe-completion-menu li.info")).toHaveText(/Session not elevated/);
        expect(await page.evaluate(() => (window as any).__elevations)).toBe(1);
      });

      test("rows use the terminal colour of their kind", async ({ page }) => {
        await page.keyboard.type("Get-Ite", { delay: 20 });
        await page.keyboard.press("Tab");
        await expect.poll(() => menuRows(page).count(), { timeout: 15_000 }).toBeGreaterThan(1);
        await page.keyboard.press("ArrowDown");
        expect(await rowColor(page, 0)).toBe("rgb(255, 255, 0)");
        await page.keyboard.press("Escape");
        await page.evaluate((sel) => (window as any).jQuery(sel).terminal().set_command("Get-Item -"), term);
        await page.keyboard.press("Tab");
        await expect.poll(() => menuRows(page).count(), { timeout: 15_000 }).toBeGreaterThan(1);
        await page.keyboard.press("ArrowDown");
        expect(await rowColor(page, 0)).toBe("rgb(255, 0, 255)");
        await page.keyboard.press("Escape");
        await page.evaluate((sel) => (window as any).jQuery(sel).terminal().set_command("$Ho"), term);
        await page.keyboard.press("Tab");
        await expect.poll(() => menuRows(page).count(), { timeout: 15_000 }).toBeGreaterThan(1);
        await page.keyboard.press("ArrowDown");
        expect(await rowColor(page, 0)).toBe("rgb(144, 238, 144)");
      });

      test("console page renders the live setting", async ({ page }) => {
        expect(await page.content()).toContain("liveAutocompletion:");
      });
    });

    test.describe("ISE terminal", () => {
      const term = iseTerm;

      test.beforeEach(async ({ page }) => {
        await openIse(page, site.url);
      });

      test("tab opens menu and enter inserts the selected entry", async ({ page }) => {
        await page.keyboard.type("Get-Ite", { delay: 20 });
        await page.keyboard.press("Tab");
        const rows = menuRows(page);
        await expect.poll(() => rows.count(), { timeout: 15_000 }).toBeGreaterThan(1);
        await expect(rows.first().locator(".text")).toHaveText(/^Get-Item/);
        const second = await rows.nth(1).locator(".text").innerText();
        await page.keyboard.press("ArrowDown");
        await page.keyboard.press("Enter");
        await expect(menu(page)).toBeHidden();
        expect(await getCommand(page, term)).toBe(second);
      });

      test("esc closes the menu and keeps the command", async ({ page }) => {
        await page.keyboard.type("Get-Ite", { delay: 20 });
        await page.keyboard.press("Tab");
        await expect(menu(page)).toBeVisible({ timeout: 15_000 });
        await page.keyboard.press("Escape");
        await expect(menu(page)).toBeHidden();
        expect(await getCommand(page, term)).toBe("Get-Ite");
      });

      test("single match inserts without a menu", async ({ page }) => {
        await page.keyboard.type("Get-ItemPropertyValu", { delay: 20 });
        await page.keyboard.press("Tab");
        await expect
          .poll(() => getCommand(page, term), { timeout: 15_000 })
          .toBe("Get-ItemPropertyValue");
        await expect(menu(page)).toBeHidden();
      });

      test("clicking a row over the editor keeps the terminal focused", async ({ page }) => {
        await page.keyboard.type("Get-Ite", { delay: 20 });
        await page.keyboard.press("Tab");
        await expect.poll(() => menuRows(page).count(), { timeout: 15_000 }).toBeGreaterThan(1);
        await page.evaluate(() => {
          const r = document.querySelector(".ace_editor")!.getBoundingClientRect();
          (window as any).jQuery(".spe-completion-menu").css({ top: r.top + 10 + "px", left: r.left + 60 + "px" });
        });
        const second = menuRows(page).nth(1);
        const text = await second.locator(".text").innerText();
        await second.click();
        await expect(menu(page)).toBeHidden();
        expect(await getCommand(page, term)).toBe(text);
        expect(await page.evaluate((sel) => (window as any).jQuery(sel).terminal().enabled(), term)).toBe(true);
        await page.keyboard.type("x");
        expect(await getCommand(page, term)).toBe(text + "x");
      });

      test("rows use the ISE foreground colour and kind colours", async ({ page }) => {
        await page.keyboard.type("[Strin", { delay: 20 });
        await page.keyboard.press("Tab");
        await expect.poll(() => menuRows(page).count(), { timeout: 15_000 }).toBeGreaterThan(1);
        await page.keyboard.press("ArrowDown");
        expect(await rowColor(page, 0)).toBe("rgb(255, 255, 255)");
        await page.keyboard.press("Escape");
        await page.evaluate((sel) => (window as any).jQuery(sel).terminal().set_command("Get-Ite"), term);
        await page.keyboard.press("Tab");
        await expect.poll(() => menuRows(page).count(), { timeout: 15_000 }).toBeGreaterThan(1);
        await page.keyboard.press("ArrowDown");
        expect(await rowColor(page, 0)).toBe("rgb(255, 255, 0)");
      });

      test("live on opens after a pause", async ({ page }) => {
        await page.evaluate(() => (window as any).spe.changeLiveAutocompletion(true));
        try {
          await page.keyboard.type("Get-Ite", { delay: 20 });
          await page.waitForTimeout(600);
          await expect(menu(page)).toBeVisible({ timeout: 15_000 });
        } finally {
          await page.evaluate(() => (window as any).spe.changeLiveAutocompletion(false));
        }
      });

      test("live is suspended while debugging, tab still works", async ({ page }) => {
        await page.evaluate((s) => {
          const editor = (window as any).ace.edit(document.querySelector(".ace_editor") as HTMLElement);
          editor.setValue(s, -1);
        }, debugScript);
        await page.evaluate(() => (window as any).scForm.postRequest("", "", "", "ise:debug"));
        try {
          await page.waitForSelector(".ace_marker-layer .breakpoint", { timeout: 30_000 });
          await page.evaluate(() => (window as any).spe.changeLiveAutocompletion(true));
          await page.locator(`${term} .cmd`).click();
          await page.keyboard.type("Get-Ite", { delay: 20 });
          await page.waitForTimeout(800);
          await expect(menu(page)).toBeHidden();
          await page.keyboard.press("Tab");
          await expect(menu(page)).toBeVisible({ timeout: 15_000 });
          await page.keyboard.press("Escape");
          await page.evaluate((sel) => (window as any).jQuery(sel).terminal().set_command(""), term);
          await page.evaluate(() =>
            (window as any).scForm.postRequest("", "", "", "ise:debugaction(action=Continue)")
          );
          await expect(page.locator(".terminal-output")).toContainText("after-resume-zzmenu", {
            timeout: 30_000,
          });
        } finally {
          await page.evaluate(() => {
            (window as any).spe.changeLiveAutocompletion(false);
            (window as any).scForm.postRequest("", "", "", "ise:debugaction(action=Quit)");
          });
        }
      });
    });
  });
}
