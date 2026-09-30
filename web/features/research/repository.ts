import "server-only"

import { z } from "zod"

import type { LabResult } from "@/core/lab-results/lab-results.types"
import type { ResearchParticipant } from "@/core/research/research.types"
import { apiGet } from "@/lib/api/server"
import {
  categoriesSchema,
  labResultsResponseSchema,
  pagedSchema,
  type Paged,
} from "@/lib/api/schemas"

const participantSchema = z.object({
  patientId: z.guid(),
  categories: categoriesSchema,
}) satisfies z.ZodType<ResearchParticipant>

export async function listParticipants(
  researcherId: string,
  page = 1
): Promise<Paged<ResearchParticipant>> {
  return apiGet(
    `/researchers/${researcherId}/patients?page=${page}`,
    pagedSchema(participantSchema)
  )
}

// Opening a participant's results is an audited read, limited to the categories they shared.
export async function listParticipantLabResults(
  researcherId: string,
  patientId: string
): Promise<LabResult[]> {
  const { items } = await apiGet(
    `/researchers/${researcherId}/patients/${patientId}/lab-results`,
    labResultsResponseSchema
  )
  return items
}
