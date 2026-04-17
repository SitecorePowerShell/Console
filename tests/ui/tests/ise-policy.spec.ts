import { test, expect, Page, Browser } from "@playwright/test";
import {
  loadTestSites,
  authStatePath,
  sitecoreLogout,
  TestSite,
} from "./sitecore-login";
import * as fs from "fs";
import * as path from "path";

const sites = loadTestSites();
const setupScript = fs.readFileSync(
  path.join(__dirname, "ise-policy-setup.ps1"),
  "utf-8"
);
const teardownScript = fs.readFileSync(
  path.join(__dirname, "ise-policy-teardown.ps1"),
  "utf-8"
);
const blockedScript = fs.readFileSync(
  path.join(__dirname, "ise-policy-blocked.ps1"),
  "utf-8"
);

const TEST_POLICY = "ui-test-block-writehost";
const POLICY_BUTTON_ID = "B8BB187CF3D5B46B8AEB2A6BDE538629C";
const POLICY_FRAME_ID = `${POLICY_BUTTON_ID}_frame`;

async function openIse(page: Page, site: TestSite) {
  await page.goto(
    `${site.url}/sitecore/shell/Applications/PowerShell/PowerShellIse`,
    { waitUntil: "domcontentloaded" }
  );
  await page.waitForSelector(".ace_editor", { timeout: 15_000 });
}

async function setEditorAndRun(page: Page, script: string) {
  await page.evaluate((s) => {
    const editor = (window as any).ace.edit(
      document.querySelector(".ace_editor") as HTMLElement
    );
    editor.setValue(s, -1);
  }, script);
  await page.keyboard.press("Control+e");
}

async function activateContextTab(page: Page) {
  await page.evaluate(() => {
    const tab = Array.from(document.querySelectorAll('[role="tab"]')).find(
      (t) => t.textContent?.trim() === "Context"
    ) as HTMLElement | undefined;
    tab?.click();
  });
}

async function openPolicyDropdown(page: Page) {
  await activateContextTab(page);
  await page.evaluate((id) => {
    const btn = document.getElementById(id);
    if (!btn) throw new Error("Policy button not in DOM");
    btn.click();
  }, POLICY_BUTTON_ID);
  // Wait for the gallery iframe to render either the treeview or the clear item.
  await page.waitForFunction(
    (frameId) => {
      const frame = document.getElementById(frameId) as HTMLIFrameElement | null;
      const doc = frame?.contentDocument;
      return !!doc?.querySelector('#PolicyTreeview a, [role="menuitem"]');
    },
    POLICY_FRAME_ID,
    { timeout: 10_000 }
  );
}

async function selectPolicyByName(page: Page, policyName: string) {
  await page.evaluate(
    ([frameId, name]) => {
      const frame = document.getElementById(frameId) as HTMLIFrameElement;
      const doc = frame.contentDocument!;
      const win = frame.contentWindow!;
      const node = Array.from(
        doc.querySelectorAll("#PolicyTreeview a")
      ).find((a) => a.textContent?.trim() === name);
      if (!node)
        throw new Error(`Tree node '${name}' not found in PolicyTreeview`);
      const ev = new (win as any).MouseEvent("click", {
        bubbles: true,
        cancelable: true,
        view: win,
      });
      node.dispatchEvent(ev);
    },
    [POLICY_FRAME_ID, policyName] as const
  );
  await page.waitForTimeout(1000);
}

async function clearPolicy(page: Page) {
  await page.evaluate((frameId) => {
    const frame = document.getElementById(frameId) as HTMLIFrameElement;
    const doc = frame.contentDocument!;
    const win = frame.contentWindow!;
    const clearItem = Array.from(
      doc.querySelectorAll('[role="menuitem"]')
    ).find((el) => el.textContent?.includes("[None]"));
    if (!clearItem) throw new Error("[None] menu item not found");
    const ev = new (win as any).MouseEvent("click", {
      bubbles: true,
      cancelable: true,
      view: win,
    });
    clearItem.dispatchEvent(ev);
  }, POLICY_FRAME_ID);
  await page.waitForTimeout(1000);
}

async function runSetupOrTeardown(
  browser: Browser,
  site: TestSite,
  script: string,
  successMarker: string
) {
  const context = await browser.newContext({
    ignoreHTTPSErrors: true,
    storageState: authStatePath(site),
  });
  const page = await context.newPage();
  try {
    await openIse(page, site);
    await setEditorAndRun(page, script);
    await expect(page.locator(".terminal-output")).toContainText(
      successMarker,
      { timeout: 60_000 }
    );
  } finally {
    await page.close();
    await context.close();
  }
}

for (const site of sites) {
  test.describe.configure({ mode: "serial" });
  test.describe(`ISE Policy Gallery - ${site.url}`, () => {
    test.use({ storageState: authStatePath(site) });

    test.beforeAll(async ({ browser }) => {
      await runSetupOrTeardown(browser, site, setupScript, "SETUP_OK");
    });

    test.afterAll(async ({ browser }) => {
      try {
        await runSetupOrTeardown(
          browser,
          site,
          teardownScript,
          "TEARDOWN_OK"
        );
      } catch {
        // best effort
      }
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

    test("Policy button shows [None] on a fresh ISE session", async ({
      page,
    }) => {
      await activateContextTab(page);
      const btn = page.locator(`#${POLICY_BUTTON_ID}`);
      await expect(btn).toBeVisible();
      await expect(btn).toContainText("[None]");
    });

    test("Dropdown renders a tree rooted at Policies plus the [None] clear entry", async ({
      page,
    }) => {
      await openPolicyDropdown(page);
      const items = await page.evaluate((frameId) => {
        const frame = document.getElementById(frameId) as HTMLIFrameElement;
        const doc = frame.contentDocument!;
        const menuItems = Array.from(
          doc.querySelectorAll('[role="menuitem"]')
        ).map((el) => el.textContent?.trim() ?? "");
        const treeNodes = Array.from(
          doc.querySelectorAll("#PolicyTreeview a")
        ).map((el) => el.textContent?.trim() ?? "");
        return { menuItems, treeNodes };
      }, POLICY_FRAME_ID);

      expect(items.menuItems.some((l) => l.includes("[None]"))).toBeTruthy();
      expect(items.treeNodes.some((l) => l === "Policies")).toBeTruthy();
      expect(items.treeNodes.some((l) => l === TEST_POLICY)).toBeTruthy();
    });

    test("Selecting a policy updates the button label", async ({ page }) => {
      await openPolicyDropdown(page);
      await selectPolicyByName(page, TEST_POLICY);

      await expect(page.locator(`#${POLICY_BUTTON_ID}`)).toContainText(
        TEST_POLICY
      );
    });

    test("Disallowed command is blocked before execution", async ({
      page,
    }) => {
      await openPolicyDropdown(page);
      await selectPolicyByName(page, TEST_POLICY);

      await setEditorAndRun(page, blockedScript);

      const terminal = page.locator(".terminal-output");
      await expect(terminal).toContainText(
        `Blocked by policy '${TEST_POLICY}'`,
        { timeout: 30_000 }
      );
      await expect(terminal).toContainText("Write-Host");
      await expect(terminal).toContainText("AllowedCommands");
      const output = await terminal.innerText();
      expect(output).not.toContain("this should be blocked");
    });

    test("[None] restores the button label and lets scripts run", async ({
      page,
    }) => {
      await openPolicyDropdown(page);
      await selectPolicyByName(page, TEST_POLICY);
      await expect(page.locator(`#${POLICY_BUTTON_ID}`)).toContainText(
        TEST_POLICY
      );

      await openPolicyDropdown(page);
      await clearPolicy(page);

      await expect(page.locator(`#${POLICY_BUTTON_ID}`)).toContainText(
        "[None]"
      );

      await setEditorAndRun(page, blockedScript);
      const terminal = page.locator(".terminal-output");
      await expect(terminal).toContainText("this should be blocked", {
        timeout: 30_000,
      });
      const output = await terminal.innerText();
      expect(output).not.toContain("Blocked by policy");
    });
  });
}
