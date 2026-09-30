export type PatientSession = {
  kind: "patient"
  patientId: string
  name: string
}

export type ClinicianSession = {
  kind: "clinician"
  clinicianId: string
  name: string
}

export type ResearcherSession = {
  kind: "researcher"
  researcherId: string
  name: string
}

export type Session = PatientSession | ClinicianSession | ResearcherSession

export type SessionKind = Session["kind"]

// Why there is, or is not, a usable session. "expired" means the portal still holds a login but the
// API refused its token (the Keycloak session ended); "unlinked" means the token is fine but the
// account is not a patient, clinician or researcher.
export type SessionState =
  | { status: "active"; session: Session }
  | { status: "expired" }
  | { status: "unlinked" }
  | { status: "signed-out" }
