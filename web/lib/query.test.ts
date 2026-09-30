import { describe, expect, it } from "vitest"

import { parseOneOf, parsePageNumber } from "./query"

describe("query parsing", () => {
  it.each([
    ["3", 3],
    ["0", 1],
    ["-2", 1],
    ["abc", 1],
    [undefined, 1],
  ])("reads page %j as %j", (value, expected) => {
    expect(parsePageNumber(value)).toBe(expected)
  })

  it("accepts only allowed values", () => {
    expect(parseOneOf("b", ["a", "b"] as const)).toBe("b")
    expect(parseOneOf("c", ["a", "b"] as const)).toBeUndefined()
    expect(parseOneOf(undefined, ["a", "b"] as const)).toBeUndefined()
  })
})
