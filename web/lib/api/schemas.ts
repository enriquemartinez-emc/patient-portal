import { z } from "zod"

import { LAB_CATEGORIES } from "@/core/lab-results/lab-results.types"
import type { LabResult } from "@/core/lab-results/lab-results.types"

// Wire shapes shared by more than one feature's repository. Ids use z.guid() because they come
// from our own API and only need to be UUID-shaped, not RFC-version-checked.
// Categories always arrive in the canonical order, so every screen lists them the same way.
export const categoriesSchema = z
  .array(z.enum(LAB_CATEGORIES))
  .transform((categories) =>
    LAB_CATEGORIES.filter((category) => categories.includes(category))
  )

export const labResultSchema = z.object({
  id: z.guid(),
  category: z.enum(LAB_CATEGORIES),
  testName: z.string(),
  value: z.number(),
  unit: z.string(),
  collectedAt: z.iso.datetime({ offset: true }),
}) satisfies z.ZodType<LabResult>

export const labResultsResponseSchema = z.object({
  items: z.array(labResultSchema),
})

export function pagedSchema<T extends z.ZodType>(item: T) {
  return z.object({
    items: z.array(item),
    page: z.number(),
    pageSize: z.number(),
    hasNextPage: z.boolean(),
  })
}

export type Paged<T> = {
  items: T[]
  page: number
  pageSize: number
  hasNextPage: boolean
}
