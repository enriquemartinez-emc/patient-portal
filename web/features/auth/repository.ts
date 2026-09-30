import "server-only"

import { z } from "zod"

import type { Session } from "@/features/auth/types"
import { ApiError, apiGet } from "@/lib/api/server"

const meSchema = z.object({
  kind: z.enum(["patient", "clinician", "researcher"]),
  id: z.guid(),
  name: z.string(),
})

// Who the signed-in login is in the portal, or null when the API has no matching record (an
// admin, or a login not linked to a patient, clinician or researcher) or no longer accepts the token.
export async function getMe(): Promise<Session | null> {
  try {
    const me = await apiGet("/me", meSchema)
    switch (me.kind) {
      case "patient":
        return { kind: "patient", patientId: me.id, name: me.name }
      case "clinician":
        return { kind: "clinician", clinicianId: me.id, name: me.name }
      case "researcher":
        return { kind: "researcher", researcherId: me.id, name: me.name }
    }
  } catch (error) {
    if (error instanceof ApiError && [401, 404, 409].includes(error.status)) {
      return null
    }
    throw error
  }
}
