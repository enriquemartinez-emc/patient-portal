import "server-only"

import { z } from "zod"

import type { SessionState } from "@/features/auth/types"
import { ApiError, apiGet } from "@/lib/api/server"

const meSchema = z.object({
  kind: z.enum(["patient", "clinician", "researcher"]),
  id: z.guid(),
  name: z.string(),
})

export async function getMe(): Promise<SessionState> {
  try {
    const me = await apiGet("/me", meSchema)
    switch (me.kind) {
      case "patient":
        return {
          status: "active",
          session: { kind: "patient", patientId: me.id, name: me.name },
        }
      case "clinician":
        return {
          status: "active",
          session: { kind: "clinician", clinicianId: me.id, name: me.name },
        }
      case "researcher":
        return {
          status: "active",
          session: {
            kind: "researcher",
            researcherId: me.id,
            name: me.name,
          },
        }
    }
  } catch (error) {
    if (error instanceof ApiError) {
      if (error.status === 401) {
        return { status: "expired" }
      }
      if ([404, 409].includes(error.status)) {
        return { status: "unlinked" }
      }
    }
    throw error
  }
}
