import "server-only"

import { z } from "zod"

import type { LabResult } from "@/core/lab-results/lab-results.types"
import { apiGet } from "@/shell/api-client"

const labResultSchema = z.object({
  id: z.guid(),
  category: z.string(),
  testName: z.string(),
  value: z.number(),
  unit: z.string(),
  collectedAt: z.iso.datetime({ offset: true }),
}) satisfies z.ZodType<LabResult>

const labResultsSchema = z.object({ items: z.array(labResultSchema) })

export async function listLabResults(patientId: string): Promise<LabResult[]> {
  const { items } = await apiGet(
    `/patients/${patientId}/lab-results`,
    labResultsSchema
  )
  return items
}
