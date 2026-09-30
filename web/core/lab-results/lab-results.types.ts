export const LAB_CATEGORIES = [
  "hematology",
  "biochemistry",
  "lipids",
  "endocrinology",
  "immunology",
  "microbiology",
  "urinalysis",
] as const

export type LabCategory = (typeof LAB_CATEGORIES)[number]

export type LabResult = {
  id: string
  category: LabCategory
  testName: string
  value: number
  unit: string
  collectedAt: string
}

export type LabResultGroup = {
  category: LabCategory
  results: readonly LabResult[]
}
