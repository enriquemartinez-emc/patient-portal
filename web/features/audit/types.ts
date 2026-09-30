export type AuditActor =
  | { kind: "patient"; name: string }
  | { kind: "clinician" | "researcher"; name: string; organization: string }

export type AuditAction =
  | { type: "lab_results_read"; labResultCount: number }
  | { type: "consent_granted"; consentId: string }
  | { type: "consent_revoked"; consentId: string }

export type AuditEntry = {
  id: string
  occurredAt: string
  actor: AuditActor
  action: AuditAction
}

export const AUDIT_ACTION_TYPES = [
  "lab_results_read",
  "consent_granted",
  "consent_revoked",
] as const

export type AuditActionType = (typeof AUDIT_ACTION_TYPES)[number]
