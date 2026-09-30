import { Card, CardContent } from "@/components/ui/card"
import { summarize } from "@/features/lab-results/summary"
import { LAB_CATEGORIES } from "@/features/lab-results/types"
import type { LabResult } from "@/features/lab-results/types"
import { formatDate } from "@/lib/format"

export function LabResultsSummary({
  results,
}: {
  results: readonly LabResult[]
}) {
  if (results.length === 0) {
    return null
  }

  const summary = summarize(results)

  return (
    <Card>
      <CardContent>
        <dl className="grid grid-cols-1 divide-y sm:grid-cols-3 sm:divide-x sm:divide-y-0">
          <div className="flex flex-col gap-1 py-3 first:pt-0 last:pb-0 sm:px-6 sm:py-0 sm:first:pl-0 sm:last:pr-0">
            <dt className="text-sm text-muted-foreground">Results on record</dt>
            <dd className="text-2xl font-semibold tabular-nums">
              {summary.total}
            </dd>
          </div>
          <div className="flex flex-col gap-1 py-3 first:pt-0 last:pb-0 sm:px-6 sm:py-0 sm:first:pl-0 sm:last:pr-0">
            <dt className="text-sm text-muted-foreground">Latest collection</dt>
            <dd className="text-2xl font-semibold tabular-nums">
              {summary.latestCollectedAt
                ? formatDate(summary.latestCollectedAt)
                : "—"}
            </dd>
          </div>
          <div className="flex flex-col gap-1 py-3 first:pt-0 last:pb-0 sm:px-6 sm:py-0 sm:first:pl-0 sm:last:pr-0">
            <dt className="text-sm text-muted-foreground">Categories</dt>
            <dd className="text-2xl font-semibold tabular-nums">
              {summary.categoryCount} of {LAB_CATEGORIES.length}
            </dd>
          </div>
        </dl>
      </CardContent>
    </Card>
  )
}
