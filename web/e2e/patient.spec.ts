import { expect, test } from "@playwright/test"

import { signIn } from "./support"

test.describe("patient", () => {
  test("sees an overview of results, sharing and access on the dashboard", async ({
    page,
  }) => {
    await signIn(page, "emily.carter@demo.example")

    await expect(page).toHaveURL("/patient")
    await expect(
      page.getByRole("heading", { name: "Welcome back, Emily" })
    ).toBeVisible()
    await expect(
      page.getByRole("link", { name: /^Lab results 26/ })
    ).toBeVisible()
    await expect(
      page.getByRole("link", { name: /^Shared with/ })
    ).toContainText("Riverside Clinic")
    for (const card of [
      "Latest results",
      "Who has opened your results",
      "Recent access",
    ]) {
      await expect(page.getByText(card, { exact: true })).toBeVisible()
    }
    await expect(page.getByRole("link", { name: "Creatinine" })).toBeVisible()
  })

  test("reads their lab results and opens one", async ({ page }) => {
    await signIn(page, "emily.carter@demo.example")

    await page.getByRole("link", { name: "Lab results", exact: true }).click()
    await expect(page).toHaveURL("/patient/lab-results")
    await expect(page.getByText("26 results on record")).toBeVisible()
    const table = page.getByRole("table")
    for (const category of [
      "hematology",
      "biochemistry",
      "lipids",
      "endocrinology",
    ]) {
      await expect(table.getByText(category).first()).toBeVisible()
    }

    await page.getByRole("link", { name: "Hemoglobin" }).first().click()
    await expect(page).toHaveURL(/\/patient\/lab-results\//)
    await expect(page.getByText("13.4 g/dL")).toBeVisible()

    await page.getByRole("link", { name: "All lab results" }).click()
    await expect(page).toHaveURL("/patient/lab-results")
  })

  test("narrows the lab results to one category", async ({ page }) => {
    await signIn(page, "emily.carter@demo.example")
    await page.goto("/patient/lab-results")

    await page
      .getByRole("navigation", { name: "Filter by category" })
      .getByRole("link", { name: /^lipids/i })
      .click()

    await expect(page).toHaveURL(/category=lipids/)
    const table = page.getByRole("table")
    await expect(table.getByText("lipids").first()).toBeVisible()
    await expect(table.getByText("hematology")).toHaveCount(0)
    await expect(table.getByText("biochemistry")).toHaveCount(0)
  })

  test("shares results, changes their mind at the dialog, then revokes and sees both in the history", async ({
    page,
  }) => {
    const purpose = `E2E study ${Date.now()}`
    await signIn(page, "emily.carter@demo.example")
    await page.getByRole("link", { name: "Consents" }).click()

    await page.locator("#organizationId").selectOption({
      label: "Meridian Research Institute (Research institution)",
    })
    await page.getByLabel("lipids").check()
    await page.getByLabel("Why are you sharing?").fill(purpose)
    await page.getByRole("button", { name: /Stop sharing on/ }).click()
    await page.getByRole("button", { name: "Go to the Next Month" }).click()
    await page.getByRole("button", { name: /15th/ }).click()
    await expect(
      page.getByRole("button", { name: /Stop sharing on/ })
    ).toContainText("15")
    await page.getByRole("button", { name: "Share results" }).click()

    await expect(
      page.getByRole("alert").filter({ hasText: "Results shared" })
    ).toBeVisible()
    const card = page.locator('[data-slot="card"]').filter({ hasText: purpose })
    await expect(card).toContainText(
      "Meridian Research Institute can view your lipids results until"
    )

    await card.getByRole("button", { name: "Revoke access" }).click()
    const dialog = page.getByRole("alertdialog")
    await expect(dialog).toContainText(
      "Revoke access for Meridian Research Institute?"
    )
    await dialog.getByRole("button", { name: "Keep access" }).click()
    await expect(dialog).toBeHidden()
    await expect(
      card.getByRole("button", { name: "Revoke access" })
    ).toBeVisible()

    await card.getByRole("button", { name: "Revoke access" }).click()
    await page
      .getByRole("alertdialog")
      .getByRole("button", { name: "Revoke access" })
      .click()
    await expect(
      page.getByRole("alert").filter({ hasText: "Access revoked" })
    ).toBeVisible()
    await expect(card).toContainText("You revoked this access")
    await expect(
      card.getByRole("button", { name: "Revoke access" })
    ).toHaveCount(0)

    await page.getByRole("link", { name: "Access history" }).click()
    await expect(page.getByText("You granted a consent.").first()).toBeVisible()
    await expect(page.getByText("You revoked a consent.").first()).toBeVisible()
  })

  test("is told what is missing when sharing without choosing any results", async ({
    page,
  }) => {
    await signIn(page, "emily.carter@demo.example")
    await page.goto("/patient/consents")

    await page.locator("#organizationId").selectOption({
      label: "Meridian Research Institute (Research institution)",
    })
    await page.getByLabel("Why are you sharing?").fill("No categories chosen")
    await page.getByRole("button", { name: "Share results" }).click()

    await expect(
      page
        .getByRole("alert")
        .filter({ hasText: "Choose at least one category" })
    ).toBeVisible()
  })

  test("filters the access history", async ({ page }) => {
    await signIn(page, "emily.carter@demo.example")
    await page.goto("/patient/access-history")

    await page.getByLabel("Show").selectOption({ label: "Consent revoked" })
    await page.getByRole("button", { name: "Apply" }).click()

    await expect(page).toHaveURL(/action=consent_revoked/)
    await expect(page.getByLabel("Show")).toHaveValue("consent_revoked")
    // The dropdown lists every type, so only rows in the table say what was filtered out.
    await expect(
      page.getByRole("table").getByText("Lab results viewed")
    ).toHaveCount(0)
  })
})
