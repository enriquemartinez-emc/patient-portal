import { requireSessionPage } from "@/features/auth/session"
import { LabCategoryFilter } from "@/features/lab-results/components/lab-category-filter"
import { LabResultsTable } from "@/features/lab-results/components/lab-results-table"
import { LabResultsSummary } from "@/features/lab-results/components/lab-results-summary"
import { parseCategoryFilter } from "@/features/lab-results/summary"
import { listLabResults } from "@/features/lab-results/repository"

export const metadata = { title: "Lab results · Patient Portal" }

export default async function PatientLabResultsPage({
  searchParams,
}: {
  searchParams: Promise<{ category?: string | string[] }>
}) {
  const session = await requireSessionPage("patient")
  const results = await listLabResults(session.patientId)
  const category = parseCategoryFilter((await searchParams).category)
  const visible = category
    ? results.filter((r) => r.category === category)
    : results

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold">Lab results</h1>
        <p className="text-sm text-muted-foreground">
          {results.length} results on record, newest first.
        </p>
      </div>
      <LabResultsSummary results={results} />
      <LabCategoryFilter
        results={results}
        selected={category}
        basePath="/patient/lab-results"
      />
      <LabResultsTable
        results={visible}
        detailHref={(result) => `/patient/lab-results/${result.id}`}
      />
    </div>
  )
}
