export type AccessBasis = "treatment" | "consent"

export type PatientSummary = {
  id: string
  fullName: string
  dateOfBirth: string
  accessBasis: readonly AccessBasis[]
}
