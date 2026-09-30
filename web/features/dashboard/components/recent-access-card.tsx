import Link from "next/link"

import {
  Card,
  CardAction,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { describeAuditEntry } from "@/features/audit/components/audit-table"
import type { AuditEntry } from "@/features/audit/types"
import { formatDateTime } from "@/lib/format"

export function RecentAccessCard({
  entries,
  className,
}: {
  entries: readonly AuditEntry[]
  className?: string
}) {
  return (
    <Card className={className}>
      <CardHeader>
        <CardTitle>Recent access</CardTitle>
        <CardAction>
          <Link
            href="/patient/access-history"
            className="text-sm text-muted-foreground hover:text-foreground"
          >
            View all
          </Link>
        </CardAction>
      </CardHeader>
      <CardContent>
        {entries.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            Nothing has been recorded yet. Every view of your results and every
            change to your consents is listed here.
          </p>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>When</TableHead>
                <TableHead>What happened</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {entries.map((entry) => (
                <TableRow key={entry.id}>
                  <TableCell className="whitespace-normal">
                    <time dateTime={entry.occurredAt}>
                      {formatDateTime(entry.occurredAt)}
                    </time>
                  </TableCell>
                  <TableCell className="whitespace-normal">
                    {describeAuditEntry(entry)}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </CardContent>
    </Card>
  )
}
