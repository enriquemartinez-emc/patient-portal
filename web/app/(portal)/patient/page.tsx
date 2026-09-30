import { LabResultsTable } from "@/features/lab-results/components/lab-results-table"
import { listLabResults } from "@/features/lab-results/repository"
import { requireSessionPage } from "@/features/auth/session"

export const metadata = { title: "Lab results · Patient Portal" }

export default async function PatientLabResultsPage() {
  const session = await requireSessionPage("patient")
  const results = await listLabResults(session.patientId)

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold">Your lab results</h1>
        <p className="text-sm text-muted-foreground">
          Welcome, {session.name}. {results.length} results on record.
        </p>
      </div>
      <LabResultsTable
        results={results}
        detailHref={(result) => `/patient/lab-results/${result.id}`}
      />
    </div>
  )
}
