import { Page, Browser } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const AUTH_DIR = path.resolve(__dirname, "../.auth");

export interface TestSite {
  url: string;
  username: string;
  password: string;
  path: string;
  version: number;
}

/**
 * Load all sites that have a "test" block defined in deploy.user.json.
 */
export function loadTestSites(): TestSite[] {
  const deployUserPath = path.resolve(
    __dirname,
    "../../../src/deploy.user.json"
  );
  const config = JSON.parse(fs.readFileSync(deployUserPath, "utf-8"));
  const sites: TestSite[] = [];
  for (const site of config.sites || []) {
    if (site.test?.url && site.test?.username && site.test?.password) {
      sites.push({
        url: site.test.url,
        username: site.test.username,
        password: site.test.password,
        path: site.path,
        version: site.version || 0,
      });
    }
  }
  if (sites.length === 0) {
    throw new Error(
      "No testable sites found. Add a test block (url, username, password) to a site in src/deploy.user.json"
    );
  }
  return sites;
}

/**
 * Log into Sitecore via Identity Server.
 * Navigates to /sitecore/shell, follows the redirect to Identity Server,
 * fills the login form, and waits for the Sitecore desktop to load.
 */
export async function sitecoreLogin(page: Page, site: TestSite) {
  await page.goto(`${site.url}/sitecore/shell`, {
    waitUntil: "domcontentloaded",
  });

  // Wait for the Identity Server login form
  await page.waitForSelector("#Username", { timeout: 15_000 });

  // Fill credentials and submit
  await page.fill("#Username", site.username);
  await page.fill("#Password", site.password);
  await page.click('button[value="login"]');

  // After login, Sitecore may show a license limit page instead of
  // the desktop. If so, click through to allow the session.
  const desktopOrLicense = await Promise.race([
    page
      .waitForSelector("#Desktop, #MainPanel, .scDesktop", { timeout: 30_000 })
      .then(() => "desktop" as const),
    page
      .waitForURL("**/LicenseOptions/**", { timeout: 30_000 })
      .then(() => "license" as const),
  ]);

  if (desktopOrLicense === "license") {
    // "Add users" temporarily adds a license seat and proceeds to the
    // desktop immediately. "Kick off user" shows a user list that
    // requires further interaction, so prefer "Add users".
    const addUsers = page.locator("text=Add users");
    if (await addUsers.isVisible()) {
      await addUsers.click();
    }
    await page.waitForSelector("#Desktop, #MainPanel, .scDesktop", {
      timeout: 30_000,
    });
  }
}

/**
 * Path to the saved auth state file for a given site.
 */
export function authStatePath(site: TestSite): string {
  const slug = new URL(site.url).hostname.replace(/[^a-zA-Z0-9]/g, "_");
  return path.join(AUTH_DIR, `${slug}.json`);
}

/**
 * Log in and save the browser storage state to disk so subsequent
 * tests can reuse the session without logging in again.
 */
export async function sitecoreLoginAndSave(
  browser: Browser,
  site: TestSite
): Promise<void> {
  fs.mkdirSync(AUTH_DIR, { recursive: true });
  const context = await browser.newContext({ ignoreHTTPSErrors: true });
  const page = await context.newPage();
  await sitecoreLogin(page, site);
  await context.storageState({ path: authStatePath(site) });
  await page.close();
  await context.close();
}

/**
 * Log out of Sitecore to free the license seat.
 * Uses the Identity Server's external logout endpoint which terminates
 * both the Sitecore session and the Identity Server session.
 */
export async function sitecoreLogout(page: Page, site: TestSite) {
  try {
    await page.goto(
      `${site.url}/sitecore/shell/federation/externallogout`,
      { waitUntil: "domcontentloaded", timeout: 10_000 }
    );
  } catch {
    // Logout is best-effort - don't fail the test if it times out
  }
  const url = new URL(site.url);
  await page.context().clearCookies({ domain: url.hostname });
}
