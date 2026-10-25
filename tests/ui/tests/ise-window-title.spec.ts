import { test, expect, Page, Frame } from "@playwright/test";
import { loadTestSites, authStatePath, sitecoreLogout } from "./sitecore-login";

const sites = loadTestSites();

// Caption layouts the title has to live with: Sitecore's own, and the kinds of restyling a
// shell theme does (modelled on the SitecoreThemes extension).
const LAYOUTS: Record<string, string> = {
  native: "",
  // Cupertino: traffic lights on the left, the title centred between symmetric padding.
  "buttons left, centred title": `
    .standardWindowCaption { height: 38px; }
    .standardWindowCaption + .windowPlaceholderContainer { top: 38px; }
    .standardWindowCaption .scWindowCaption { display: block; line-height: 38px; text-align: center; padding: 0 92px; }
    .standartWindowButtons { display: inline-flex; gap: 8px; top: 0; height: 38px; left: 14px; right: auto; }`,
  // Buttons moved to the left with no room made for them in the caption.
  "buttons left, unpadded title": `.standartWindowButtons { left: 4px; right: auto; }`,
  "large buttons": `.standartWindowButtons img { width: 80px !important; height: 30px !important; }`,
  // Bridge: an absolutely placed flex caption followed by a decorative bar.
  "flex caption with trailing bar": `
    .standardWindowCaption { position: relative; box-sizing: border-box; height: 44px; }
    .standardWindowCaption .scWindowCaption { position: absolute; top: 0; bottom: 0; left: 14px; right: 128px; display: flex; align-items: center; gap: 10px; padding: 0; font-size: 24px; line-height: 1; white-space: nowrap; }
    .standardWindowCaption .scWindowCaption::after { content: ""; flex: 1; min-width: 20px; height: 12px; background: #f96; }
    .standartWindowButtons { top: 12px; right: 14px; height: 20px; margin: 0; }`,
};

async function waitForFrame(page: Page, re: RegExp, selector: string): Promise<Frame> {
  for (let i = 0; i < 120; i++) {
    const f = page.frames().find((x) => re.test(x.url()));
    if (f && (await f.$(selector).catch(() => null))) return f;
    await page.waitForTimeout(250);
  }
  throw new Error(`frame ${re} not found: ` + page.frames().map((f) => f.url()).join(" | "));
}

for (const site of sites) {
  test.describe(`ISE desktop window title - ${site.url}`, () => {
    test.use({ storageState: authStatePath(site) });

    test.afterAll(async ({ browser }) => {
      const context = await browser.newContext({ ignoreHTTPSErrors: true, storageState: authStatePath(site) });
      const page = await context.newPage();
      await sitecoreLogout(page, site);
      await context.close();
    });

    for (const [layout, css] of Object.entries(LAYOUTS)) for (const variant of ["", "modified", "ISE reloaded"]) test(`long title drops the app name, then cuts the path start (${layout}${variant && ", " + variant})`, async ({ page }) => {
      await page.goto(`${site.url}/sitecore/shell/default.aspx`, { waitUntil: "domcontentloaded" });
      await page.waitForFunction(() => (window as any).scManager, null, { timeout: 30_000 });
      await page.evaluate(() => {
        const td = Array.from(document.querySelectorAll("td.scMenuItemCaption")).find((e) => e.textContent === "PowerShell ISE");
        (td!.closest("tr") as HTMLElement).click();
      });
      let ise = await waitForFrame(page, /PowerShellIse/i, ".ace_editor");
      const titleReady = () => ise.waitForFunction(() => {
        const title = window.parent.document.querySelector("#WindowCaption");
        return title?.querySelector(".speWindowPath") && getComputedStyle(title).display === "inline-flex";
      }, null, { timeout: 15_000 });
      await titleReady();
      if (variant === "ISE reloaded") {
        // The window keeps the caption set up by the first ISE load; the reloaded ISE has to reuse it.
        await ise.evaluate(() => location.reload());
        await page.waitForTimeout(1000);
        ise = await waitForFrame(page, /PowerShellIse/i, ".ace_editor");
        await titleReady();
      }
      if (variant === "modified") {
        // An edit shows the modified mark in front of the path.
        await ise.locator(".ace_editor").first().click();
        await page.keyboard.type("#");
        await ise.waitForFunction(() => getComputedStyle(window.parent.document.querySelector("#WindowCaption .scriptModified")!).display !== "none", null, { timeout: 15_000 });
      }

      // Restyle the window after the ISE has laid out its title, the way a theme switch would,
      // and give the observers a frame to re-measure.
      await ise.evaluate(async (css) => {
        const style = window.parent.document.createElement("style");
        style.textContent = css;
        window.parent.document.head.appendChild(style);
        await new Promise((r) => requestAnimationFrame(() => requestAnimationFrame(r)));
      }, css);

      // Grows the title one segment at a time through the real ISE title path and
      // records the layout at the first step where each fallback kicks in.
      const result = await ise.evaluate(() => {
        const spe = (window as any).spe;
        const doc = window.parent.document;
        const title = doc.getElementById("WindowCaption")!;
        const index = +/ModifiedMark(\d+)/.exec(title.innerHTML)![1];
        // applyWindowTitle rewrites the title, so look its parts up each time.
        const part = (sel: string) => title.querySelector(sel) as HTMLElement;
        const buttons = doc.querySelector(".standartWindowButtons")!.getBoundingClientRect();
        // A dropped app name has wrapped onto the clipped second line.
        const appVisible = () => {
          const box = title.getBoundingClientRect();
          return part(".speWindowAppName").getBoundingClientRect().top < box.top + box.height / 2;
        };
        const pathCut = () => part(".speWindowPath").scrollWidth > part(".speWindowPath").clientWidth;
        const snap = () => {
          const box = title.getBoundingClientRect(), path = part(".speWindowPath");
          return {
            appVisible: appVisible(),
            markVisible: part(".scriptModified").getBoundingClientRect().width > 0,
            pathOnFirstLine: Math.abs(path.getBoundingClientRect().top - box.top) < box.height / 2,
            pathCut: pathCut(),
            docOverflow: doc.documentElement.scrollWidth - doc.documentElement.clientWidth,
            clearOfButtons: box.right <= buttons.left || box.left >= buttons.right || box.bottom <= buttons.top || box.top >= buttons.bottom,
            endVisible: path.firstElementChild!.getBoundingClientRect().right <= path.getBoundingClientRect().right + 1,
          };
        };
        const set = (n: number) => {
          spe.changeTabDetails(index, "/sitecore/system/Modules/PowerShell" + "/Lib".repeat(n) + "/Last", "", "", "master");
          spe.applyWindowTitle(index);
        };
        set(0);
        const short = snap();
        let dropped = null, cut = null;
        for (let n = 1; n < 400 && !cut; n++) {
          set(n);
          if (!dropped && !appVisible()) dropped = snap();
          if (pathCut()) cut = snap();
        }
        return { short, dropped, cut };
      });

      expect(result.short.markVisible).toBe(variant === "modified");
      expect(result.short).toMatchObject({ appVisible: true, pathCut: false, docOverflow: 0, pathOnFirstLine: true, clearOfButtons: true });
      expect(result.dropped).toMatchObject({ appVisible: false, pathCut: false, docOverflow: 0, pathOnFirstLine: true, clearOfButtons: true });
      expect(result.cut).toMatchObject({ appVisible: false, pathCut: true, docOverflow: 0, pathOnFirstLine: true, clearOfButtons: true, endVisible: true });

      await page.goto("about:blank").catch(() => {});
    });
  });
}
