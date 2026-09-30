import { expect, test } from "@playwright/test"

import { asPerson, signIn } from "./support"

test.describe("clinician", () => {
  test("a treating clinician opens a patient, and the patient can see that they did", async ({
    browser,
    page,
  }) => {
    await signIn(page, "sarah.thompson@demo.example")
    await expect(page).toHaveURL("/clinician")
    await expect(page.getByText("You treat this patient")).toBeVisible()

    await page.getByRole("link", { name: "Emily Carter" }).click()
    await expect(page).toHaveURL(/\/clinician\/patients\//)
    await expect(page.getByText("This access is recorded")).toBeVisible()
    for (const category of [
      "hematology",
      "biochemistry",
      "lipids",
      "endocrinology",
    ]) {
      await expect(
        page.getByRole("table").getByText(category).first()
      ).toBeVisible()
    }

    await asPerson(browser, "emily.carter@demo.example", async (emily) => {
      await emily.goto("/patient/access-history")
      await expect(
        emily
          .getByText(
            "Dr. Sarah Thompson (Northside Clinic) viewed 26 lab results."
          )
          .first()
      ).toBeVisible()

      await emily.goto("/patient")
      await expect(
        emily.getByRole("img", {
          name: /Times your results were opened each day/,
        })
      ).toBeVisible()
      await expect(
        emily
          .getByText(
            "Dr. Sarah Thompson (Northside Clinic) viewed 26 lab results."
          )
          .first()
      ).toBeVisible()
    })
  })

  test("a clinician with only a consent sees just the categories the patient shared", async ({
    page,
  }) => {
    await signIn(page, "michael.brown@demo.example")
    await expect(page.getByText("Shared with your organization")).toBeVisible()

    await page.getByRole("link", { name: "Emily Carter" }).click()

    const table = page.getByRole("table")
    await expect(table.getByText("hematology").first()).toBeVisible()
    await expect(table.getByText("lipids").first()).toBeVisible()
    await expect(table.getByText("biochemistry")).toHaveCount(0)
    await expect(table.getByText("endocrinology")).toHaveCount(0)
  })

  test("a patient who is not theirs is not found", async ({ page }) => {
    await signIn(page, "sarah.thompson@demo.example")

    // James Wilson: her treatment ended and he has not shared with her clinic.
    await page.goto("/clinician/patients/b0000000-0000-0000-0000-000000000002")

    await expect(
      page.getByRole("heading", { name: "Page not found" })
    ).toBeVisible()
  })
})
