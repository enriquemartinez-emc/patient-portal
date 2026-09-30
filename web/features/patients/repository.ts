import "server-only"

import { z } from "zod"

import type { LabResult } from "@/core/lab-results/lab-results.types"
import type { PatientSummary } from "@/core/patients/patients.types"
import { apiGet, isNotFound } from "@/lib/api/server"
import {
  labResultsResponseSchema,
  pagedSchema,
  type Paged,
} from "@/lib/api/schemas"

const patientSummarySchema = z.object({
  id: z.guid(),
  fullName: z.string(),
  dateOfBirth: z.iso.date(),
  accessBasis: z.array(z.enum(["treatment", "consent"])),
}) satisfies z.ZodType<PatientSummary>

export async function listMyPatients(
  clinicianId: string,
  page = 1
): Promise<Paged<PatientSummary>> {
  return apiGet(
    `/clinicians/${clinicianId}/patients?page=${page}`,
    pagedSchema(patientSummarySchema)
  )
}

export async function getMyPatient(
  clinicianId: string,
  patientId: string
): Promise<PatientSummary | null> {
  try {
    return await apiGet(
      `/clinicians/${clinicianId}/patients/${patientId}`,
      patientSummarySchema
    )
  } catch (error) {
    if (isNotFound(error)) {
      return null
    }
    throw error
  }
}

// Opening a patient's results is an audited read: the API records it for the patient.
export async function listPatientLabResults(
  clinicianId: string,
  patientId: string
): Promise<LabResult[]> {
  const { items } = await apiGet(
    `/clinicians/${clinicianId}/patients/${patientId}/lab-results`,
    labResultsResponseSchema
  )
  return items
}
