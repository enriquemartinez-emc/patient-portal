import Link from "next/link"

import { Card } from "@/components/ui/card"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import type { LabResult } from "@/features/lab-results/types"
import { sortNewestFirst } from "@/features/lab-results/summary"
import { formatDate } from "@/lib/format"

import { CategoryBadge } from "./category-label"

// Flat table of results sorted newest first. Pass detailHref to make each test name a link.
export function LabResultsTable({
  results,
  detailHref,
}: {
  results: readonly LabResult[]
  detailHref?: (result: LabResult) => string
}) {
  if (results.length === 0) {
    return <p className="text-muted-foreground">No lab results to show.</p>
  }

  const sorted = sortNewestFirst(results)

  return (
    <Card className="overflow-hidden py-0">
      <Table className="table-fixed">
        <TableHeader>
          <TableRow>
            <TableHead className="w-[38%] sm:w-[32%]">Test</TableHead>
            <TableHead className="w-[37%] sm:w-[24%]">Category</TableHead>
            <TableHead className="w-[25%] sm:w-[20%]">Result</TableHead>
            <TableHead className="hidden sm:table-cell">Collected</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {sorted.map((result) => (
            <TableRow key={result.id}>
              <TableCell className="whitespace-normal">
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
                <time
                  className="block text-xs text-muted-foreground sm:hidden"
                  dateTime={result.collectedAt}
                >
                  {formatDate(result.collectedAt)}
                </time>
              </TableCell>
              <TableCell className="px-1 whitespace-normal sm:px-2">
                <CategoryBadge category={result.category} />
              </TableCell>
              <TableCell className="font-semibold tabular-nums">
                {result.value}{" "}
                <span className="text-muted-foreground">{result.unit}</span>
              </TableCell>
              <TableCell className="hidden sm:table-cell">
                <time dateTime={result.collectedAt}>
                  {formatDate(result.collectedAt)}
                </time>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </Card>
  )
}
