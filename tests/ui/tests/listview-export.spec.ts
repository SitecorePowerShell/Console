import { test, expect, Page, Frame } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";
import { loadTestSites, authStatePath, sitecoreLogout, TestSite } from "./sitecore-login";

const sites = loadTestSites();
// Same content as the filter tests: a few folders under spe-test shown in a ListView.
const showScript = fs.readFileSync(path.join(__dirname, "listview-filter.ps1"), "utf-8");
const teardownScript = fs.readFileSync(path.join(__dirname, "listview-filter-teardown.ps1"), "utf-8");
const fileNameScript = fs.readFileSync(path.join(__dirname, "listview-export-filename.ps1"), "utf-8");

async function openIse(page: Page, site: TestSite) {
  await page.goto(`${site.url}/sitecore/shell/Applications/PowerShell/PowerShellIse`, { waitUntil: "domcontentloaded" });
  await page.waitForSelector(".ace_editor", { timeout: 15_000 });
  await page.evaluate((id) => (window as any).scForm.postRequest("", "", "", `ise:setsessionid(id=${id})`), `listview-export-${Date.now()}`);
}

async function run(page: Page, script: string) {
  await page.evaluate((s) => {
    const editor = (window as any).ace.edit(document.querySelector(".ace_editor") as HTMLElement);
    editor.setValue(s, -1);
  }, script);
  await page.locator(".ace_editor").first().click();
  await page.keyboard.press("Control+e");
}

// See listview-filter.spec.ts: the ListView only opens via its Application handle URL.
async function openListView(page: Page, site: TestSite, script = showScript): Promise<Frame> {
  const src = new Promise<string>((resolve, reject) => {
    const t = setTimeout(() => reject(new Error("no Broadcast seen")), 60_000);
    page.on("response", async (r) => {
      if (r.request().method() !== "POST") return;
      const txt = await r.text().catch(() => "");
      const m = txt.match(/Src=\\"([^"]*xmlcontrol=Application[^"]*)\\"/);
      if (m) { clearTimeout(t); resolve(m[1].replace(/&amp;/g, "&")); }
    });
  });
  await run(page, script);
  const lvPage = await page.context().newPage();
  await lvPage.goto(site.url + (await src), { waitUntil: "domcontentloaded" });
  for (let i = 0; i < 120; i++) {
    const f = lvPage.frames().find((x) => /PowerShell(ListView|ResultViewerList)/i.test(x.url()));
    if (f && (await f.$(".scRibbonToolbarLargeComboButton"))) return f;
    await lvPage.waitForTimeout(250);
  }
  throw new Error("ListView export button not found: " + lvPage.frames().map((f) => f.url()).join(" | "));
}

const exportButton = (lv: Frame) =>
  lv.locator(".scRibbonToolbarLargeComboButton").filter({ has: lv.locator("a[onclick*='export:results']") });

async function exportCsvFileName(lv: Frame): Promise<string> {
  await exportButton(lv).locator(".scRibbonToolbarLargeComboButtonBottom").click();
  const csv = lv.frameLocator("#ExportResultsButton_frame").locator(".scMenuPanelItem").filter({ hasText: /^\s*CSV\s*$/ });
  await expect(csv).toBeVisible({ timeout: 15_000 });
  const download = lv.page().waitForEvent("download", { timeout: 60_000 });
  await csv.click();
  return (await download).suggestedFilename();
}

for (const site of sites) {
  test.describe(`ListView export - ${site.url}`, () => {
    test.use({ storageState: authStatePath(site) });

    test.afterAll(async ({ browser }) => {
      const context = await browser.newContext({ ignoreHTTPSErrors: true, storageState: authStatePath(site) });
      const page = await context.newPage();
      await openIse(page, site);
      await run(page, teardownScript);
      await expect(page.locator(".terminal-output")).toContainText("TEARDOWN_OK", { timeout: 60_000 });
      await sitecoreLogout(page, site);
      await context.close();
    });

    test("exporters render as one split button that remembers the last pick", async ({ page }) => {
      await openIse(page, site);
      const lv = await openListView(page, site);

      await expect(exportButton(lv)).toHaveCount(1);
      // No leftover small buttons, one per exporter
      await expect(lv.locator(".scRibbonToolbarSmallButton[onclick*='export:results']")).toHaveCount(0);

      await exportButton(lv).locator(".scRibbonToolbarLargeComboButtonBottom").click();
      // The dropdown is Gallery.ExportGallery, loaded into an iframe next to the button
      const gallery = lv.frameLocator("#ExportResultsButton_frame");
      const items = gallery.locator(".scMenuPanelItem");
      await expect(items.first()).toBeVisible({ timeout: 15_000 });
      // Opens tall enough for every exporter, no scrollbar
      const overflowing = await gallery.locator("body").evaluate(() =>
        Array.from(document.querySelectorAll<HTMLElement>("*"))
          // body is overflow:hidden, so only scrollable containers can show a scrollbar
          .filter((el) => /auto|scroll/.test(getComputedStyle(el).overflowY))
          .filter((el) => el.clientHeight > 0 && el.scrollHeight > el.clientHeight + 1)
          .map((el) => `${el.tagName}#${el.id}.${el.className} ${el.scrollHeight}>${el.clientHeight}`));
      expect(overflowing).toEqual([]);
      const headers = (await items.allInnerTexts()).map((h) => h.trim());
      expect(headers.length).toBeGreaterThan(1);
      const current = (await exportButton(lv).locator(".header").innerText()).trim();
      await expect(gallery.locator(".scMenuPanelItem.selected")).toHaveText(current);
      const pick = headers.find((h) => h && h !== current)!;

      await items.filter({ hasText: pick }).first().click();
      await expect(exportButton(lv).locator(".header")).toHaveText(pick, { timeout: 30_000 });
      await expect(exportButton(lv).locator(".scRibbonToolbarLargeComboButtonTop")).toHaveAttribute("onclick", /export:results/);
    });

    test("export file name defaults to report-<timestamp>", async ({ page }) => {
      await openIse(page, site);
      const lv = await openListView(page, site);
      expect(await exportCsvFileName(lv)).toMatch(/^report-\d{4}-\d{2}-\d{2}_\d{6}\.csv$/);
    });

    test("-ExportFileName formats the export date and strips invalid characters", async ({ page }) => {
      await openIse(page, site);
      const lv = await openListView(page, site, fileNameScript);
      expect(await exportCsvFileName(lv)).toMatch(/^spe_export-\d{8}\.csv$/);
    });
  });
}
