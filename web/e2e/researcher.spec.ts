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
    await expect(
      page.getByRole("heading", { name: "biochemistry" })
    ).toBeVisible()
    await expect(page.getByText("HbA1c")).toBeVisible()
    await expect(page.getByRole("heading", { name: "hematology" })).toHaveCount(
      0
    )
    await expect(page.getByRole("heading", { name: "urinalysis" })).toHaveCount(
      0
    )
  })
})
