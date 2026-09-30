// Who is acting. Each persona carries only the identifier its API routes need.
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

export type NavItem = {
  label: string
  href: string
}
