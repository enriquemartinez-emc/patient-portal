import { describe, expect, it } from "vitest"

import { dateToValue, valueToDate } from "@/lib/date-value"

describe("dateToValue", () => {
  it("writes the calendar day the person picked, padded", () => {
    expect(dateToValue(new Date(2026, 9, 5))).toBe("2026-10-05")
    expect(dateToValue(new Date(2026, 0, 31))).toBe("2026-01-31")
  })

  it("keeps the picked day whatever the time of day", () => {
    expect(dateToValue(new Date(2026, 11, 31, 23, 59))).toBe("2026-12-31")
    expect(dateToValue(new Date(2027, 0, 1, 0, 0))).toBe("2027-01-01")
  })
})

describe("valueToDate", () => {
  it("reads a date value as that calendar day in the local time zone", () => {
    const date = valueToDate("2026-10-05")

    expect(date.getFullYear()).toBe(2026)
    expect(date.getMonth()).toBe(9)
    expect(date.getDate()).toBe(5)
  })

  it("round-trips, including a leap day", () => {
    for (const value of ["2026-10-05", "2028-02-29", "2026-12-31"]) {
      expect(dateToValue(valueToDate(value))).toBe(value)
    }
  })
})
