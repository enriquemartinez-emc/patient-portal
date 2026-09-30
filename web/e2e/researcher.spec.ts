import { expect, test } from "@playwright/test"

import { signIn } from "./support"

test.describe("researcher", () => {
  test("sees participants without names and only the categories they shared", async ({
    page,
  }) => {
    await signIn(page, "laura.davies@demo.example")
    await expect(page).toHaveURL("/researcher")

    await expect(page.getByText("James Wilson")).toHaveCount(0)
    await expect(page.getByText("Emily Carter")).toHaveCount(0)

    await page.getByRole("link", { name: "Participant 00000002" }).click()
    await expect(page.getByText("This access is recorded")).toBeVisible()
    const table = page.getByRole("table")
    await expect(table.getByText("biochemistry").first()).toBeVisible()
    await expect(table.getByText("HbA1c").first()).toBeVisible()
    await expect(table.getByText("hematology")).toHaveCount(0)
    await expect(table.getByText("urinalysis")).toHaveCount(0)
  })
})
