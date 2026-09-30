import Link from "next/link"

import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  categoryLabel,
  formatResultValue,
  groupByCategory,
} from "@/core/lab-results/lab-results.rules"
import type { LabResult } from "@/core/lab-results/lab-results.types"
import { formatDate } from "@/core/shared/dates"

// Results grouped by category. Pass detailHref to make each test name a link.
export function LabResultsTable({
  results,
  detailHref,
}: {
  results: readonly LabResult[]
  detailHref?: (result: LabResult) => string
}) {
  const groups = groupByCategory(results)

  if (groups.length === 0) {
    return <p className="text-muted-foreground">No lab results to show.</p>
  }

  return (
    <div className="flex flex-col gap-8">
      {groups.map((group) => (
        <section key={group.category} aria-labelledby={`cat-${group.category}`}>
          <h2 id={`cat-${group.category}`} className="mb-2 text-lg font-medium">
            {categoryLabel(group.category)}
          </h2>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Test</TableHead>
                <TableHead>Result</TableHead>
                <TableHead>Collected</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {group.results.map((result) => (
                <TableRow key={result.id}>
                  <TableCell>
                    {detailHref ? (
                      <Link
                        href={detailHref(result)}
                        className="font-medium underline-offset-4 hover:underline"
                      >
                        {result.testName}
                      </Link>
                    ) : (
                      result.testName
                    )}
                  </TableCell>
                  <TableCell>{formatResultValue(result)}</TableCell>
                  <TableCell>
                    <time dateTime={result.collectedAt}>
                      {formatDate(result.collectedAt)}
                    </time>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </section>
      ))}
    </div>
  )
}
