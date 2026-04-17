import { test, expect, Page } from "@playwright/test";
import {
  loadTestSites,
  authStatePath,
  sitecoreLogout,
  TestSite,
} from "./sitecore-login";

const sites = loadTestSites();

// The Session gallery button id is derived from the core-db ribbon item id
// (ISE/Ribbon/Script/Context/Session) prefixed with "B".
const SESSION_BUTTON_ID = "B0C784F542B464EE2B0BA72384125E123";
const SESSION_FRAME_ID = `${SESSION_BUTTON_ID}_frame`;

async function openIse(page: Page, site: TestSite) {
  await page.goto(
    `${site.url}/sitecore/shell/Applications/PowerShell/PowerShellIse`,
    { waitUntil: "domcontentloaded" }
  );
  await page.waitForSelector(".ace_editor", { timeout: 15_000 });
}

async function activateContextTab(page: Page) {
  await page.evaluate(() => {
    const tab = Array.from(document.querySelectorAll('[role="tab"]')).find(
      (t) => t.textContent?.trim() === "Context"
    ) as HTMLElement | undefined;
    tab?.click();
  });
}

// ActiveTabs persistence remembers the last-selected ribbon strip across
// ISE reloads, so a prior test that ended on Context would otherwise
// start the next test on Context too. Wait for the restore postback
// before forcing Home so we don't race the server-driven strip switch.
async function resetToHomeTab(page: Page) {
  await page.waitForTimeout(300);
  await page.evaluate(() => {
    const tab = Array.from(document.querySelectorAll('[role="tab"]')).find(
      (t) => t.textContent?.trim() === "Home"
    ) as HTMLElement | undefined;
    tab?.click();
  });
}

async function openSessionDropdown(page: Page) {
  await activateContextTab(page);
  await page.evaluate((id) => {
    document.getElementById(id)?.click();
  }, SESSION_BUTTON_ID);
  await page.waitForFunction(
    (frameId) => {
      const frame = document.getElementById(frameId) as HTMLIFrameElement | null;
      const doc = frame?.contentDocument;
      return !!doc?.querySelector(".scMenuPanelItem.speSessionRow");
    },
    SESSION_FRAME_ID,
    { timeout: 10_000 }
  );
}

for (const site of sites) {
  test.describe(`ISE Session dropdown - ${site.url}`, () => {
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
      await resetToHomeTab(page);
    });

    test("Active session id pill reads as a solid teal chip", async ({
      page,
    }) => {
      await openSessionDropdown(page);
      const pillStyle = await page.evaluate((frameId) => {
        const frame = document.getElementById(frameId) as HTMLIFrameElement;
        const doc = frame.contentDocument!;
        const pill = doc.querySelector(
          ".speSessionRowSelected .speSessionIdPill"
        );
        if (!pill) return null;
        const cs = frame.contentWindow!.getComputedStyle(pill);
        return {
          borderColor: cs.borderColor,
          backgroundColor: cs.backgroundColor,
          color: cs.color,
          borderRadius: cs.borderRadius,
        };
      }, SESSION_FRAME_ID);

      // Active row inverts the pill: solid #267F98 bg, white text, same 3px
      // radius as the non-active rows. Compare as rgb() substrings so the
      // assertion doesn't care whether the browser reports colours as #hex
      // or rgb().
      expect(pillStyle).not.toBeNull();
      expect(pillStyle!.backgroundColor).toContain("38, 127, 152");
      expect(pillStyle!.borderColor).toContain("38, 127, 152");
      expect(pillStyle!.color).toContain("255, 255, 255");
      expect(pillStyle!.borderRadius).toBe("3px");
    });

    test("Active session row has the expanded-variable accent highlight", async ({
      page,
    }) => {
      await openSessionDropdown(page);
      const row = await page.evaluate((frameId) => {
        const frame = document.getElementById(frameId) as HTMLIFrameElement;
        const doc = frame.contentDocument!;
        const selected = doc.querySelector(
          ".scMenuPanelItem.speSessionRow.speSessionRowSelected"
        );
        if (!selected) return null;
        const cs = frame.contentWindow!.getComputedStyle(selected);
        return {
          borderLeftColor: cs.borderLeftColor,
          // parseFloat avoids asserting on exact "2px" - device pixel ratio /
          // zoom rounds the computed value to e.g. "1.6px" at 80% zoom.
          borderLeftWidth: parseFloat(cs.borderLeftWidth),
        };
      }, SESSION_FRAME_ID);

      expect(row).not.toBeNull();
      // Saturated teal accent (#267F98) in place of the old pale hairline -
      // makes the active row unambiguous when peer sessions share the id.
      expect(row!.borderLeftColor).toContain("38, 127, 152");
      expect(row!.borderLeftWidth).toBeGreaterThan(2);
    });

    test("Active row reserves the kill slot so pill alignment is stable", async ({
      page,
    }) => {
      await openSessionDropdown(page);
      const rows = await page.evaluate((frameId) => {
        const frame = document.getElementById(frameId) as HTMLIFrameElement;
        const doc = frame.contentDocument!;
        return Array.from(
          doc.querySelectorAll(".scMenuPanelItem.speSessionRow")
        ).map((r) => ({
          selected: r.className.includes("speSessionRowSelected"),
          hasKillButton: !!r.querySelector(".speSessionKill"),
          hasKillSlot: !!r.querySelector(".speSessionKillSlot"),
        }));
      }, SESSION_FRAME_ID);

      const active = rows.find((r) => r.selected);
      expect(active).toBeDefined();
      // The active row must not let the user delete the session they are bound
      // to, but it still reserves the 18px slot so the pill column stays lined
      // up with the non-active rows.
      expect(active!.hasKillButton).toBe(false);
      expect(active!.hasKillSlot).toBe(true);
    });
  });
}
