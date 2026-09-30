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
import { CategoryBadge } from "@/features/lab-results/components/category-label"
import type { LabResult } from "@/features/lab-results/types"
import { formatDate } from "@/lib/format"

export function LatestResultsCard({
  results,
  className,
}: {
  results: readonly LabResult[]
  className?: string
}) {
  return (
    <Card className={className}>
      <CardHeader>
        <CardTitle>Latest results</CardTitle>
        <CardAction>
          <Link
            href="/patient/lab-results"
            className="text-sm text-muted-foreground hover:text-foreground"
          >
            View all
          </Link>
        </CardAction>
      </CardHeader>
      <CardContent>
        {results.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            No lab results yet. They appear here when a lab adds them.
          </p>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Test</TableHead>
                <TableHead className="hidden sm:table-cell">Kind</TableHead>
                <TableHead>Result</TableHead>
                <TableHead>Collected</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {results.map((result) => (
                <TableRow key={result.id}>
                  <TableCell>
                    <Link
                      href={`/patient/lab-results/${result.id}`}
                      className="font-medium underline-offset-4 hover:underline"
                    >
                      {result.testName}
                    </Link>
                  </TableCell>
                  <TableCell className="hidden sm:table-cell">
                    <CategoryBadge category={result.category} />
                  </TableCell>
                  <TableCell>
                    {result.value} {result.unit}
                  </TableCell>
                  <TableCell>
                    <time dateTime={result.collectedAt}>
                      {formatDate(result.collectedAt)}
                    </time>
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
