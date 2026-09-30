import { describe, expect, it } from "vitest"

import { formatDate, formatDateTime, joinWithAnd } from "./dates"

describe("dates", () => {
  it("formats a date in UTC regardless of the offset in the input", () => {
    expect(formatDate("2030-10-12T23:30:00-05:00")).toBe("13 Oct 2030")
  })

  it("formats a date and time and says it is UTC", () => {
    expect(formatDateTime("2030-10-12T09:30:00Z")).toBe(
      "12 Oct 2030, 09:30 UTC"
    )
  })
})

describe("joinWithAnd", () => {
  it.each([
    [[], ""],
    [["a"], "a"],
    [["a", "b"], "a and b"],
    [["a", "b", "c"], "a, b and c"],
  ])("joins %j as %j", (items, expected) => {
    expect(joinWithAnd(items)).toBe(expected)
  })
})
