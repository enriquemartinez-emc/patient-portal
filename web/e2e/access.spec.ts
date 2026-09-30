import { expect, test } from "@playwright/test"

import { logOut, signIn } from "./support"

test.describe("access", () => {
  test("signed-out visitors are sent to sign in", async ({ page }) => {
    await page.goto("/patient")

    await expect(page).toHaveURL("/login")
    await expect(page.getByRole("button", { name: "Sign in" })).toBeVisible()
  })

  test("a patient cannot open the clinician area", async ({ page }) => {
    await signIn(page, "emily.carter@demo.example")

    await page.goto("/clinician")

    await expect(page).toHaveURL("/patient")
  })

  test("a login with no portal record is told so and can sign out", async ({
    page,
  }) => {
    await signIn(page, "admin@demo.example")

    await expect(page).toHaveURL("/login")
    await expect(
      page.getByText("This account has no portal access")
    ).toBeVisible()
    await page.getByRole("button", { name: "Log out" }).click()

    await expect(page.getByRole("button", { name: "Sign in" })).toBeVisible()
  })

  test("a wrong password stays on the Keycloak page with an error", async ({
    page,
  }) => {
    await signIn(page, "emily.carter@demo.example", "not-the-password")

    await expect(page.getByText("Invalid username or password")).toBeVisible()
  })

  test("signing out ends the session and the next sign-in asks for credentials again", async ({
    page,
  }) => {
    await signIn(page, "emily.carter@demo.example")
    await expect(page).toHaveURL("/patient")

    await logOut(page)
    await expect(page).toHaveURL("/login")

    await page.goto("/patient")
    await expect(page).toHaveURL("/login")
    await page.getByRole("button", { name: "Sign in" }).click()
    await expect(page.locator("#username")).toBeVisible()
  })
})

test.describe("on a phone", () => {
  test.use({ viewport: { width: 390, height: 844 } })

  test("the navigation stays usable and the page does not scroll sideways", async ({
    page,
  }) => {
    await signIn(page, "emily.carter@demo.example")
    await page.goto("/patient/consents")

    // Check no horizontal scroll with the sidebar closed
    const scrollsSideways = await page.evaluate(
      () =>
        document.documentElement.scrollWidth >
        document.documentElement.clientWidth
    )
    expect(scrollsSideways).toBe(false)

    // Open the sidebar sheet
    await page.getByRole("button", { name: "Toggle Sidebar" }).click()

    // Check that navigation links and account menu are visible
    await expect(
      page.getByRole("link", { name: "Access history" })
    ).toBeVisible()
    await expect(
      page.getByRole("button", { name: "Account menu" })
    ).toBeVisible()
  })
})
