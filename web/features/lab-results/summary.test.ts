import { describe, expect, it } from "vitest"

import {
  groupByCategory,
  parseCategoryFilter,
  sortNewestFirst,
  summarize,
} from "@/features/lab-results/summary"
import type { LabResult } from "@/features/lab-results/types"

function result(overrides: Partial<LabResult>): LabResult {
  return {
    id: "1",
    category: "hematology",
    testName: "Hemoglobin",
    value: 13.4,
    unit: "g/dL",
    collectedAt: "2026-05-02T08:30:00+00:00",
    ...overrides,
  }
}

describe("groupByCategory", () => {
  it("follows the fixed category order and leaves out empty categories", () => {
    const groups = groupByCategory([
      result({ id: "a", category: "urinalysis" }),
      result({ id: "b", category: "lipids" }),
      result({ id: "c", category: "lipids" }),
    ])

    expect(groups.map((group) => group.category)).toEqual([
      "lipids",
      "urinalysis",
    ])
    expect(groups[0].results.map((r) => r.id)).toEqual(["b", "c"])
  })

  it("returns no groups without results", () => {
    expect(groupByCategory([])).toEqual([])
  })
})

describe("sortNewestFirst", () => {
  it("puts the newest collection first", () => {
    const sorted = sortNewestFirst([
      result({ id: "a", collectedAt: "2026-02-10T08:40:00+00:00" }),
      result({ id: "b", collectedAt: "2026-08-14T08:00:00+00:00" }),
      result({ id: "c", collectedAt: "2025-11-04T08:15:00+00:00" }),
    ])

    expect(sorted.map((r) => r.id)).toEqual(["b", "a", "c"])
  })

  it("orders results from the same collection by test name", () => {
    const sorted = sortNewestFirst([
      result({ id: "a", testName: "TSH" }),
      result({ id: "b", testName: "Hemoglobin" }),
      result({ id: "c", testName: "LDL cholesterol" }),
    ])

    expect(sorted.map((r) => r.testName)).toEqual([
      "Hemoglobin",
      "LDL cholesterol",
      "TSH",
    ])
  })

  it("compares instants, not text, when offsets differ", () => {
    const sorted = sortNewestFirst([
      result({ id: "a", collectedAt: "2026-08-15T00:30:00+02:00" }),
      result({ id: "b", collectedAt: "2026-08-14T23:00:00+00:00" }),
    ])

    expect(sorted.map((r) => r.id)).toEqual(["b", "a"])
  })

  it("does not change the list it was given", () => {
    const input = [
      result({ id: "a", collectedAt: "2025-01-01T00:00:00+00:00" }),
      result({ id: "b", collectedAt: "2026-01-01T00:00:00+00:00" }),
    ]

    sortNewestFirst(input)

    expect(input.map((r) => r.id)).toEqual(["a", "b"])
  })
})

describe("summarize", () => {
  it("counts results and distinct categories and finds the latest collection", () => {
    const summary = summarize([
      result({ id: "a", collectedAt: "2026-05-02T08:30:00+00:00" }),
      result({
        id: "b",
        category: "biochemistry",
        collectedAt: "2026-08-14T08:00:00+00:00",
      }),
      result({ id: "c", collectedAt: "2026-06-01T08:00:00+00:00" }),
    ])

    expect(summary).toEqual({
      total: 3,
      categoryCount: 2,
      latestCollectedAt: "2026-08-14T08:00:00+00:00",
    })
  })

  it("compares instants, not text, when offsets differ", () => {
    const summary = summarize([
      result({ id: "a", collectedAt: "2026-08-14T23:00:00+00:00" }),
      result({ id: "b", collectedAt: "2026-08-15T00:30:00+02:00" }),
    ])

    expect(summary.latestCollectedAt).toBe("2026-08-14T23:00:00+00:00")
  })

  it("has no latest date without results", () => {
    expect(summarize([])).toEqual({
      total: 0,
      categoryCount: 0,
      latestCollectedAt: null,
    })
  })
})

describe("parseCategoryFilter", () => {
  it("accepts a known category", () => {
    expect(parseCategoryFilter("lipids")).toBe("lipids")
  })

  it("uses the first value when the parameter repeats", () => {
    expect(parseCategoryFilter(["urinalysis", "lipids"])).toBe("urinalysis")
  })

  it("ignores missing or unknown values", () => {
    expect(parseCategoryFilter(undefined)).toBeNull()
    expect(parseCategoryFilter("")).toBeNull()
    expect(parseCategoryFilter("Lipids")).toBeNull()
    expect(parseCategoryFilter("nonsense")).toBeNull()
  })
})
