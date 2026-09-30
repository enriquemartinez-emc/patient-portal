import "server-only"

import type { LabResult } from "@/features/lab-results/types"
import { apiGet, isNotFound } from "@/lib/api/server"
import {
  labResultSchema,
  labResultsResponseSchema,
} from "@/features/lab-results/schemas"

export async function listLabResults(patientId: string): Promise<LabResult[]> {
  const { items } = await apiGet(
    `/patients/${patientId}/lab-results`,
    labResultsResponseSchema
  )
  return items
}

export async function getLabResult(
  patientId: string,
  labResultId: string
): Promise<LabResult | null> {
  try {
    return await apiGet(
      `/patients/${patientId}/lab-results/${labResultId}`,
      labResultSchema
    )
  } catch (error) {
    if (isNotFound(error)) {
      return null
    }
    throw error
  }
}
