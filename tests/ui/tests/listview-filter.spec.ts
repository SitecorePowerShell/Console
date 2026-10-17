import { test, expect, Page, Frame } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";
import { loadTestSites, authStatePath, sitecoreLogout, TestSite } from "./sitecore-login";

const sites = loadTestSites();
const showScript = fs.readFileSync(path.join(__dirname, "listview-filter.ps1"), "utf-8");
const teardownScript = fs.readFileSync(path.join(__dirname, "listview-filter-teardown.ps1"), "utf-8");

async function openIse(page: Page, site: TestSite) {
  await page.goto(`${site.url}/sitecore/shell/Applications/PowerShell/PowerShellIse`, { waitUntil: "domcontentloaded" });
  await page.waitForSelector(".ace_editor", { timeout: 15_000 });
  await page.evaluate((id) => (window as any).scForm.postRequest("", "", "", `ise:setsessionid(id=${id})`), `listview-filter-${Date.now()}`);
}

async function run(page: Page, script: string) {
  await page.evaluate((s) => {
    const editor = (window as any).ace.edit(document.querySelector(".ace_editor") as HTMLElement);
    editor.setValue(s, -1);
  }, script);
  await page.locator(".ace_editor").first().click();
  await page.keyboard.press("Control+e");
}

// Show-ListView (non-modal) answers the ISE poll with a Sheer Broadcast that
// inserts an Application iframe into the Desktop's Shell frame. The standalone
// ISE has no Shell frame, so grab the iframe src from the response and open it
// in its own page instead.
async function openListView(page: Page, site: TestSite): Promise<Frame> {
  const src = new Promise<string>((resolve, reject) => {
    const t = setTimeout(() => reject(new Error("no Broadcast seen")), 60_000);
    page.on("response", async (r) => {
      if (r.request().method() !== "POST") return;
      const txt = await r.text().catch(() => "");
      const m = txt.match(/Src=\\"([^"]*xmlcontrol=Application[^"]*)\\"/);
      if (m) { clearTimeout(t); resolve(m[1].replace(/&amp;/g, "&")); }
    });
  });
  await run(page, showScript);
  const lvPage = await page.context().newPage();
  await lvPage.goto(site.url + (await src), { waitUntil: "domcontentloaded" });
  for (let i = 0; i < 120; i++) {
    const f = lvPage.frames().find((x) => /PowerShell(ListView|ResultViewerList)/i.test(x.url()));
    if (f && (await f.$("#Input_Filter"))) return f;
    await lvPage.waitForTimeout(250);
  }
  throw new Error("ListView frame not found: " + lvPage.frames().map((f) => f.url()).join(" | "));
}

async function inspect(lv: Frame) {
  return lv.evaluate(() => {
    const el = document.getElementById("Input_Filter") as HTMLInputElement | null;
    return {
      value: el?.value ?? null,
      itemCount: document.getElementById("ItemCount")?.textContent,
    };
  });
}

async function filter(lv: Frame, text: string) {
  const pg = lv.page();
  const navs: string[] = [];
  pg.on("framenavigated", (f) => navs.push(f.url()));
  await lv.click("#Input_Filter");
  await pg.keyboard.type(text);
  await pg.keyboard.press("Enter");
  await lv.waitForTimeout(2000);
  const after = await inspect(lv);
  // A full postback (implicit form submit) re-runs the initial load and
  // resets both the filter box and the list.
  expect(navs).toEqual([]);
  expect(after.value).toBe(text);
  expect(after.itemCount).toBe("2");
}

for (const site of sites) {
  test.describe(`ListView filter - ${site.url}`, () => {
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

    test("Enter in the filter box filters the list", async ({ page }) => {
      await openIse(page, site);
      const lv = await openListView(page, site);
      await filter(lv, "Layout");
    });

    test("filter still works after a row click re-rendered the ribbon", async ({ page }) => {
      await openIse(page, site);
      const lv = await openListView(page, site);
      // Selecting a row raises SelectionChanged, which re-renders the ribbon
      // and brings back the input's native inline onchange/onkeydown.
      await lv.locator("[id^='lvi']").first().click();
      await lv.waitForTimeout(1500);
      await filter(lv, "Layout");
    });
  });
}
