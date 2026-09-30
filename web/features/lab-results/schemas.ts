import { z } from "zod"

import { LAB_CATEGORIES } from "@/features/lab-results/types"
import type { LabResult } from "@/features/lab-results/types"

// Ids use z.guid(): they come from our own API and only need to be UUID-shaped.

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
