import "server-only"

import { z } from "zod"

import type { PatientSummary } from "@/core/patients/patients.types"
import { apiGet } from "@/shell/api-client"

const patientSummarySchema = z.object({
  id: z.guid(),
  fullName: z.string(),
  dateOfBirth: z.iso.date(),
  accessBasis: z.array(z.enum(["treatment", "consent"])),
}) satisfies z.ZodType<PatientSummary>

const pageSchema = z.object({
  items: z.array(patientSummarySchema),
  page: z.number(),
  pageSize: z.number(),
  hasNextPage: z.boolean(),
})

export async function listMyPatients(
  clinicianId: string,
  page = 1
): Promise<z.infer<typeof pageSchema>> {
  return apiGet(`/clinicians/${clinicianId}/patients?page=${page}`, pageSchema)
}
