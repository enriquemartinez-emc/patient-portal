import { expect, test } from "@playwright/test"

import { daysAhead, signIn } from "./support"

test.describe("patient", () => {
  test("reads their lab results and opens one", async ({ page }) => {
    await signIn(page, "emily.carter@demo.example")

    await expect(page).toHaveURL("/patient")
    await expect(page.getByText("4 results on record")).toBeVisible()
    for (const category of [
      "hematology",
      "biochemistry",
      "lipids",
      "endocrinology",
    ]) {
      await expect(page.getByRole("heading", { name: category })).toBeVisible()
    }

    await page.getByRole("link", { name: "Hemoglobin" }).click()
    await expect(page).toHaveURL(/\/patient\/lab-results\//)
    await expect(page.getByText("13.4 g/dL")).toBeVisible()

    await page.getByRole("link", { name: "All lab results" }).click()
    await expect(page).toHaveURL("/patient")
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
    await page.getByLabel(/Stop sharing on/).fill(daysAhead(30))
    await page.getByRole("button", { name: "Share results" }).click()

    await expect(
      page.getByRole("alert").filter({ hasText: "Results shared" })
    ).toBeVisible()
    const card = page.locator('[data-slot="card"]').filter({ hasText: purpose })
    await expect(card).toContainText(
      "Meridian Research Institute can view your lipids results until"
    )

    // Opening the dialog and keeping access changes nothing.
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

    // Confirming revokes it.
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
