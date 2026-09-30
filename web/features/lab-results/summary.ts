import { LAB_CATEGORIES } from "@/features/lab-results/types"
import type {
  LabCategory,
  LabResult,
  LabResultGroup,
} from "@/features/lab-results/types"

// Results grouped by category in the fixed category order, leaving out empty categories.
export function groupByCategory(
  results: readonly LabResult[]
): LabResultGroup[] {
  return LAB_CATEGORIES.map((category) => ({
    category,
    results: results.filter((result) => result.category === category),
  })).filter((group) => group.results.length > 0)
}

// Newest collection first. Results collected together (one blood draw) are ordered by test name,
// so the order never depends on how the API happened to return them.
export function sortNewestFirst(results: readonly LabResult[]): LabResult[] {
  return [...results].sort(
    (a, b) =>
      Date.parse(b.collectedAt) - Date.parse(a.collectedAt) ||
      a.testName.localeCompare(b.testName) ||
      a.id.localeCompare(b.id)
  )
}

export type LabResultsSummary = {
  total: number
  categoryCount: number
  latestCollectedAt: string | null
}

export function summarize(results: readonly LabResult[]): LabResultsSummary {
  const latest = results.reduce<LabResult | null>(
    (newest, result) =>
      newest === null ||
      Date.parse(result.collectedAt) > Date.parse(newest.collectedAt)
        ? result
        : newest,
    null
  )

  return {
    total: results.length,
    categoryCount: new Set(results.map((result) => result.category)).size,
    latestCollectedAt: latest?.collectedAt ?? null,
  }
}

// The category named in a `?category=` search parameter, or null when it is missing or not one of
// ours, so a hand-edited address shows everything instead of nothing.
export function parseCategoryFilter(
  value: string | string[] | undefined
): LabCategory | null {
  const first = Array.isArray(value) ? value[0] : value
  return LAB_CATEGORIES.find((category) => category === first) ?? null
}
