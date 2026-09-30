import { expect, test } from "@playwright/test"

import { openAccountMenu, signIn } from "./support"

test.describe("account menu", () => {
  test("shows who is signed in and their role", async ({ page }) => {
    await signIn(page, "sarah.thompson@demo.example")

    await openAccountMenu(page)

    const menu = page.getByRole("menu")
    await expect(menu).toContainText("Dr. Sarah Thompson")
    await expect(menu).toContainText("clinician")
  })

  test("changes the theme and remembers it", async ({ page }) => {
    await signIn(page, "emily.carter@demo.example")
    const html = page.locator("html")

    await openAccountMenu(page)
    await page.getByRole("menuitemradio", { name: "Dark" }).click()
    await expect(html).toHaveClass(/\bdark\b/)

    await page.reload()
    await expect(html).toHaveClass(/\bdark\b/)

    await openAccountMenu(page)
    await expect(
      page.getByRole("menuitemradio", { name: "Dark" })
    ).toBeChecked()
    await page.getByRole("menuitemradio", { name: "Light" }).click()
    await expect(html).not.toHaveClass(/\bdark\b/)
  })

  test("opens the settings page", async ({ page }) => {
    await signIn(page, "emily.carter@demo.example")

    await openAccountMenu(page)
    await page.getByRole("menuitem", { name: "Settings" }).click()

    await expect(page).toHaveURL("/settings")
    await expect(page.getByRole("heading", { name: "Settings" })).toBeVisible()
  })

  test("settings needs a signed-in user", async ({ page }) => {
    await page.goto("/settings")

    await expect(page).toHaveURL("/login")
  })
})
