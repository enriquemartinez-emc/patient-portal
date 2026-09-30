import { describe, expect, it } from "vitest"

import { formatDate, formatDateTime, humanize, joinWithAnd } from "./format"

describe("format", () => {
  it("formats a date in UTC regardless of the offset in the input", () => {
    expect(formatDate("2030-10-12T23:30:00-05:00")).toBe("13 Oct 2030")
  })

  it("formats a date and time and says it is UTC", () => {
    expect(formatDateTime("2030-10-12T09:30:00Z")).toBe(
      "12 Oct 2030, 09:30 UTC"
    )
  })

  it.each([
    [[], ""],
    [["a"], "a"],
    [["a", "b"], "a and b"],
    [["a", "b", "c"], "a, b and c"],
  ])("joins %j as %j", (items, expected) => {
    expect(joinWithAnd(items)).toBe(expected)
  })

  it.each([
    ["hematology", "Hematology"],
    ["research_institution", "Research institution"],
    ["", ""],
  ])("humanizes %j as %j", (value, expected) => {
    expect(humanize(value)).toBe(expected)
  })
})
