import { test, expect } from "@playwright/test";
import {
  loadTestSites,
  sitecoreLoginAndSave,
  authStatePath,
} from "./sitecore-login";

const sites = loadTestSites();

for (const site of sites) {
  test(`Sitecore warmup - ${site.url}`, async ({ browser }) => {
    await sitecoreLoginAndSave(browser, site);

    // Verify the auth state was saved
    const fs = await import("fs");
    expect(fs.existsSync(authStatePath(site))).toBe(true);
  });
}
