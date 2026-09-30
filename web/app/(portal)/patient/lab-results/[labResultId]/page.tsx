import Link from "next/link"
import { notFound } from "next/navigation"

import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { requireSessionPage } from "@/features/auth/session"
import { getLabResult } from "@/features/lab-results/repository"
import { formatDateTime, humanize } from "@/lib/format"

export const metadata = { title: "Lab result · Patient Portal" }

export default async function LabResultPage({
  params,
}: {
  params: Promise<{ labResultId: string }>
}) {
  const { labResultId } = await params
  const session = await requireSessionPage("patient")
  const result = await getLabResult(session.patientId, labResultId)
  if (!result) {
    notFound()
  }

  return (
    <div className="flex flex-col gap-6">
      <Link
        href="/patient"
        className="text-sm text-muted-foreground hover:text-foreground"
      >
        ← All lab results
      </Link>
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-3 text-xl">
            {result.testName}
            <Badge variant="secondary">{humanize(result.category)}</Badge>
          </CardTitle>
        </CardHeader>
        <CardContent>
          <dl className="grid grid-cols-[auto_1fr] gap-x-6 gap-y-2 text-sm">
            <dt className="text-muted-foreground">Result</dt>
            <dd className="font-medium">
              {result.value} {result.unit}
            </dd>
            <dt className="text-muted-foreground">Collected</dt>
            <dd>
              <time dateTime={result.collectedAt}>
                {formatDateTime(result.collectedAt)}
              </time>
            </dd>
          </dl>
        </CardContent>
      </Card>
    </div>
  )
}
