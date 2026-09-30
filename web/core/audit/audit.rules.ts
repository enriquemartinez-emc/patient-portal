import { AUDIT_ACTION_TYPES } from "./audit.types"
import type { AuditActionType, AuditActor, AuditEntry } from "./audit.types"

function actorLabel(actor: AuditActor): string {
  return actor.kind === "patient"
    ? "You"
    : `${actor.name} (${actor.organization})`
}

// One line a patient can read: who did what.
export function describeAuditEntry(entry: AuditEntry): string {
  const who = actorLabel(entry.actor)

  switch (entry.action.type) {
    case "lab_results_read": {
      const count = entry.action.labResultCount
      return count === 0
        ? `${who} asked to view your lab results, but none were available to share.`
        : `${who} viewed ${count} lab ${count === 1 ? "result" : "results"}.`
    }
    case "consent_granted":
      return `${who} granted a consent.`
    case "consent_revoked":
      return `${who} revoked a consent.`
  }
}

export function auditActionLabel(type: AuditActionType): string {
  switch (type) {
    case "lab_results_read":
      return "Lab results viewed"
    case "consent_granted":
      return "Consent granted"
    case "consent_revoked":
      return "Consent revoked"
  }
}

// Untrusted query-string value -> a known filter, or undefined for "all".
export function parseAuditActionFilter(
  value: string | undefined
): AuditActionType | undefined {
  return AUDIT_ACTION_TYPES.find((type) => type === value)
}

// Untrusted query-string value -> a page number of at least 1.
export function parsePageNumber(value: string | undefined): number {
  const page = Number.parseInt(value ?? "", 10)
  return Number.isSafeInteger(page) && page >= 1 ? page : 1
}
