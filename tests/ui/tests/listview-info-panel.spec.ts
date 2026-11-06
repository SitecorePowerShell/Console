import { test, expect, Page, Frame } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";
import { loadTestSites, authStatePath, refreshSitecoreSession, TestSite } from "./sitecore-login";

const sites = loadTestSites();
const showScript = fs.readFileSync(path.join(__dirname, "listview-info-panel.ps1"), "utf-8");

async function openIse(page: Page, site: TestSite) {
  await page.goto(`${site.url}/sitecore/shell/Applications/PowerShell/PowerShellIse`, { waitUntil: "domcontentloaded" });
  await page.waitForSelector(".ace_editor", { timeout: 15_000 });
  await page.evaluate((id) => (window as any).scForm.postRequest("", "", "", `ise:setsessionid(id=${id})`), `listview-info-panel-${Date.now()}`);
}

async function run(page: Page, script: string) {
  await page.evaluate((s) => {
    const editor = (window as any).ace.edit(document.querySelector(".ace_editor") as HTMLElement);
    editor.setValue(s, -1);
  }, script);
  await page.locator(".ace_editor").first().click();
  await page.keyboard.press("Control+e");
}

// See listview-filter.spec.ts: the standalone ISE has no Shell frame, so open
// the Application iframe src from the Broadcast response in its own page.
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
    if (f && (await f.$("#InfoPanel"))) return f;
    await lvPage.waitForTimeout(250);
  }
  throw new Error("ListView frame not found: " + lvPage.frames().map((f) => f.url()).join(" | "));
}

for (const site of sites) {
  test.describe(`ListView info panel - ${site.url}`, () => {
    test.use({ storageState: authStatePath(site) });

    test.afterAll(async ({ browser }) => {
      const context = await browser.newContext({ ignoreHTTPSErrors: true, storageState: authStatePath(site) });
      const page = await context.newPage();
      await refreshSitecoreSession(page, site);
      await context.close();
    });

    test("InfoTitle and InfoDescription use the information palette, not the warning one", async ({ page }) => {
      await openIse(page, site);
      const lv = await openListView(page, site);
      const colors = await lv.evaluate(() => {
        const color = (selector: string) => {
          const el = document.querySelector(selector);
          return el ? getComputedStyle(el).color : null;
        };
        return {
          background: getComputedStyle(document.getElementById("InfoPanel")!).backgroundColor,
          title: color(".scEditorInformationTitle"),
          description: color(".scEditorInformationHelp"),
        };
      });
      expect(colors.background).toBe("rgb(217, 237, 247)");
      expect(colors.title).toBe("rgb(28, 110, 145)");
      expect(colors.description).toBe("rgb(28, 110, 145)");
    });
  });
}
