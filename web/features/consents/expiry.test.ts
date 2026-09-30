import { describe, expect, it } from "vitest"

import { earliestExpiryDate } from "./expiry"

describe("earliestExpiryDate", () => {
  it("is tomorrow in UTC, across month and year ends", () => {
    expect(earliestExpiryDate(new Date("2026-09-29T22:30:00Z"))).toBe(
      "2026-09-30"
    )
    expect(earliestExpiryDate(new Date("2026-09-30T00:00:00Z"))).toBe(
      "2026-10-01"
    )
    expect(earliestExpiryDate(new Date("2026-12-31T23:59:59Z"))).toBe(
      "2027-01-01"
    )
  })
})
