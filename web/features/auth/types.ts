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
