import { describe, expect, it } from "vitest"

import { formatDate, formatDateTime } from "./format"

describe("format", () => {
  it("formats a date in UTC regardless of the offset in the input", () => {
    expect(formatDate("2030-10-12T23:30:00-05:00")).toBe("13 Oct 2030")
  })

  it("formats a date and time and says it is UTC", () => {
    expect(formatDateTime("2030-10-12T09:30:00Z")).toBe(
      "12 Oct 2030, 09:30 UTC"
    )
  })
})
