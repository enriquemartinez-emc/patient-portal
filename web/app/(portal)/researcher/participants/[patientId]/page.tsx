import Link from "next/link"

import { AccessDenied } from "@/components/access-denied"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { requireSessionPage } from "@/features/auth/session"
import { LabResultsTable } from "@/features/lab-results/components/lab-results-table"
import { listParticipantLabResults } from "@/features/research/repository"
import { isForbidden } from "@/lib/api/server"
import { participantLabel } from "@/features/research/participant-label"

export const metadata = { title: "Participant results · Patient Portal" }

export default async function ResearcherParticipantPage({
  params,
}: {
  params: Promise<{ patientId: string }>
}) {
  const { patientId } = await params
  const session = await requireSessionPage("researcher")

  let results
  try {
    // This request is the audited read, limited to the categories the participant shared.
    results = await listParticipantLabResults(session.researcherId, patientId)
  } catch (error) {
    if (isForbidden(error)) {
      return <AccessDenied />
    }
    throw error
  }

  return (
    <div className="flex flex-col gap-6">
      <Link
        href="/researcher"
        className="text-sm text-muted-foreground hover:text-foreground"
      >
        ← All participants
      </Link>
      <h1 className="text-2xl font-semibold">{participantLabel(patientId)}</h1>
      <Alert>
        <AlertTitle>This access is recorded</AlertTitle>
        <AlertDescription>
          Opening these results is added to the participant&apos;s access
          history. Only the categories they shared are shown.
        </AlertDescription>
      </Alert>
      <LabResultsTable results={results} />
    </div>
  )
}
