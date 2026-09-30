import type {
  AuditActionType,
  AuditActor,
  AuditEntry,
} from "@/core/audit/audit.types"

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

export const auditActionLabels: Record<AuditActionType, string> = {
  lab_results_read: "Lab results viewed",
  consent_granted: "Consent granted",
  consent_revoked: "Consent revoked",
}
