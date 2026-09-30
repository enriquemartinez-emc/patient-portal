import { LAB_CATEGORIES } from "./lab-results.types"
import type {
  LabCategory,
  LabResult,
  LabResultGroup,
} from "./lab-results.types"

export function categoryLabel(category: LabCategory): string {
  switch (category) {
    case "hematology":
      return "Hematology"
    case "biochemistry":
      return "Biochemistry"
    case "lipids":
      return "Lipids"
    case "endocrinology":
      return "Endocrinology"
    case "immunology":
      return "Immunology"
    case "microbiology":
      return "Microbiology"
    case "urinalysis":
      return "Urinalysis"
  }
}

export function formatResultValue(result: LabResult): string {
  return `${result.value} ${result.unit}`
}

// Groups results by category in the fixed category order, keeping each group's incoming order
// and leaving out categories that have no results.
export function groupByCategory(
  results: readonly LabResult[]
): LabResultGroup[] {
  return LAB_CATEGORIES.map((category) => ({
    category,
    results: results.filter((result) => result.category === category),
  })).filter((group) => group.results.length > 0)
}

// Categories in the fixed category order, so the same set always reads the same way.
export function sortCategories(
  categories: readonly LabCategory[]
): LabCategory[] {
  return LAB_CATEGORIES.filter((category) => categories.includes(category))
}
