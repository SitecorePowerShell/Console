import { test } from "@playwright/test";
import { loadTestSites, authStatePath, sitecoreLogout } from "./sitecore-login";

// Teardown for the warmup login: every spec shares that one session, so it is
// ended once here rather than per spec file.
for (const site of loadTestSites()) {
  test(`Sitecore logout - ${site.url}`, async ({ browser }) => {
    const context = await browser.newContext({
      ignoreHTTPSErrors: true,
      storageState: authStatePath(site),
    });
    await sitecoreLogout(await context.newPage(), site);
    await context.close();
  });
}
