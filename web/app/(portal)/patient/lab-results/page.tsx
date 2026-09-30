import { requireSessionPage } from "@/features/auth/session"
import { LabResultsTable } from "@/features/lab-results/components/lab-results-table"
import { listLabResults } from "@/features/lab-results/repository"

export const metadata = { title: "Lab results · Patient Portal" }

export default async function PatientLabResultsPage() {
  const session = await requireSessionPage("patient")
  const results = await listLabResults(session.patientId)

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold">Lab results</h1>
        <p className="text-sm text-muted-foreground">
          {results.length} results on record, grouped by kind of test.
        </p>
      </div>
      <LabResultsTable
        results={results}
        detailHref={(result) => `/patient/lab-results/${result.id}`}
      />
    </div>
  )
}
