import type { Browser, Page } from "@playwright/test"

export const baseURL = process.env.E2E_BASE_URL ?? "http://localhost:3000"

// Signs in through the real Keycloak login page.
export async function signIn(
  page: Page,
  email: string,
  password = "demo-password"
) {
  await page.goto("/login")
  await page.getByRole("button", { name: "Sign in" }).click()
  await page.locator("#username").fill(email)
  await page.locator("#password").fill(password)
  await page.locator("#kc-login").click()
}

export async function openAccountMenu(page: Page) {
  await page.getByRole("button", { name: "Account menu" }).click()
}

export async function logOut(page: Page) {
  await openAccountMenu(page)
  await page.getByRole("menuitem", { name: "Log out" }).click()
}

// A second signed-in person in their own browser context, for tests where one person's action is
// checked from another's screen. The context is closed when the callback returns.
export async function asPerson<T>(
  browser: Browser,
  email: string,
  run: (page: Page) => Promise<T>
): Promise<T> {
  const context = await browser.newContext({ baseURL })
  try {
    const page = await context.newPage()
    await signIn(page, email)
    return await run(page)
  } finally {
    await context.close()
  }
}

// A yyyy-mm-dd date this many days ahead (UTC).
export function daysAhead(days: number): string {
  const date = new Date()
  date.setUTCDate(date.getUTCDate() + days)
  return date.toISOString().slice(0, 10)
}
