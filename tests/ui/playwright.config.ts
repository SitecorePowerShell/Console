import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "./tests",
  timeout: 90_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  retries: 0,
  workers: 1,
  reporter: "list",
  use: {
    ignoreHTTPSErrors: true,
    screenshot: "only-on-failure",
    trace: "retain-on-failure",
  },
  projects: [
    {
      name: "setup",
      testMatch: /00-warmup\.spec\.ts/,
      teardown: "teardown",
    },
    {
      name: "teardown",
      testMatch: /99-logout\.spec\.ts/,
    },
    {
      name: "chromium",
      use: { browserName: "chromium" },
      dependencies: ["setup"],
      testIgnore: /(00-warmup|99-logout)\.spec\.ts/,
    },
  ],
  outputDir: "./test-results",
});
