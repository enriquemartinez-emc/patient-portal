import { defineConfig, devices } from "@playwright/test"

import { baseURL } from "./e2e/support"

// Runs against a live stack (`docker compose up`). Tests change shared data (consents), so they run
// one at a time.
export default defineConfig({
  testDir: "./e2e",
  fullyParallel: false,
  workers: 1,
  timeout: 60_000,
  expect: { timeout: 10_000 },
  reporter: [["list"]],
  use: {
    baseURL,
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
})
