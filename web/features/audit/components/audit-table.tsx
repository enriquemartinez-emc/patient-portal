import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import type { AuditEntry } from "@/core/audit/audit.types"
import { auditActionLabels, describeAuditEntry } from "@/features/audit/view"
import { formatDateTime } from "@/lib/format"

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
              {describeAuditEntry(entry)}
            </TableCell>
            <TableCell>{auditActionLabels[entry.action.type]}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}
