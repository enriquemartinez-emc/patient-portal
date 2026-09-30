import "server-only"

import type { Session } from "@/core/session/session.types"

// Development only: the people created by the API's dev seed (api/PatientPortal.Migrations/Scripts/DevSeed).
// Replaced by the real identity provider when authentication is added.
export const devPersonas: readonly Session[] = [
  {
    kind: "patient",
    patientId: "b0000000-0000-0000-0000-000000000001",
    name: "Alma Reyes",
  },
  {
    kind: "patient",
    patientId: "b0000000-0000-0000-0000-000000000002",
    name: "Tomas Berg",
  },
  {
    kind: "clinician",
    clinicianId: "c0000000-0000-0000-0000-000000000001",
    name: "Dr. Ines Okafor",
  },
  {
    kind: "clinician",
    clinicianId: "c0000000-0000-0000-0000-000000000002",
    name: "Dr. Paul Lindqvist",
  },
  {
    kind: "researcher",
    researcherId: "d0000000-0000-0000-0000-000000000001",
    name: "Dr. Mei Tanaka",
  },
]

export function personaId(session: Session): string {
  switch (session.kind) {
    case "patient":
      return session.patientId
    case "clinician":
      return session.clinicianId
    case "researcher":
      return session.researcherId
  }
}

export function findDevPersona(id: string): Session | undefined {
  return devPersonas.find((persona) => personaId(persona) === id)
}
