import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import type {
  AuditActionType,
  AuditActor,
  AuditEntry,
} from "@/features/audit/types"
import { formatDateTime } from "@/lib/format"

export const auditActionLabels: Record<AuditActionType, string> = {
  lab_results_read: "Lab results viewed",
  consent_granted: "Consent granted",
  consent_revoked: "Consent revoked",
}

function actorLabel(actor: AuditActor): string {
  return actor.kind === "patient"
    ? "You"
    : `${actor.name} (${actor.organization})`
}

// One line a patient can read: who did what.
function describe(entry: AuditEntry): string {
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

export function AuditTable({ entries }: { entries: readonly AuditEntry[] }) {
  if (entries.length === 0) {
    return (
      <p className="text-muted-foreground">Nothing has been recorded yet.</p>
    )
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>When</TableHead>
          <TableHead>What happened</TableHead>
          <TableHead>Type</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {entries.map((entry) => (
          <TableRow key={entry.id}>
            <TableCell className="whitespace-nowrap">
              <time dateTime={entry.occurredAt}>
                {formatDateTime(entry.occurredAt)}
              </time>
            </TableCell>
            <TableCell className="whitespace-normal">
              {describe(entry)}
            </TableCell>
            <TableCell>{auditActionLabels[entry.action.type]}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}
