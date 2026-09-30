import { describe, expect, it } from "vitest"

import {
  categoryLabel,
  formatResultValue,
  groupByCategory,
  sortCategories,
} from "./lab-results.rules"
import { LAB_CATEGORIES } from "./lab-results.types"
import type { LabCategory, LabResult } from "./lab-results.types"

const result = (id: string, category: LabCategory): LabResult => ({
  id,
  category,
  testName: `Test ${id}`,
  value: 13.4,
  unit: "g/dL",
  collectedAt: "2026-05-02T08:30:00Z",
})

describe("lab results", () => {
  it("has a label for every category", () => {
    for (const category of LAB_CATEGORIES) {
      expect(categoryLabel(category)).toMatch(/^[A-Z]/)
    }
  })

  it("formats a value with its unit", () => {
    expect(formatResultValue(result("1", "hematology"))).toBe("13.4 g/dL")
  })

  it("groups by category in the fixed order, keeping incoming order within a group", () => {
    const groups = groupByCategory([
      result("1", "urinalysis"),
      result("2", "hematology"),
      result("3", "urinalysis"),
    ])

    expect(groups.map((g) => g.category)).toEqual(["hematology", "urinalysis"])
    expect(groups[1].results.map((r) => r.id)).toEqual(["1", "3"])
  })

  it("leaves out categories without results", () => {
    expect(groupByCategory([])).toEqual([])
  })

  it("sorts categories into the fixed order and drops repeats", () => {
    expect(
      sortCategories(["urinalysis", "lipids", "hematology", "lipids"])
    ).toEqual(["hematology", "lipids", "urinalysis"])
  })
})
