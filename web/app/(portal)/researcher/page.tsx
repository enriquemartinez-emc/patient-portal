import { PaginationNav } from "@/components/pagination-nav"
import { requireSessionPage } from "@/features/auth/session"
import { ParticipantsTable } from "@/features/research/components/participants-table"
import { listParticipants } from "@/features/research/repository"
import { parsePageNumber } from "@/lib/query"

export const metadata = { title: "Participants · Patient Portal" }

export default async function ResearcherParticipantsPage({
  searchParams,
}: {
  searchParams: Promise<{ page?: string }>
}) {
  const session = await requireSessionPage("researcher")
  const page = parsePageNumber((await searchParams).page)
  const participants = await listParticipants(session.researcherId, page)

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold">Consented participants</h1>
        <p className="text-sm text-muted-foreground">
          Welcome, {session.name}. Patients who have shared results with your
          institution. You see only the categories they chose, and no names.
        </p>
      </div>
      <ParticipantsTable participants={participants.items} />
      <PaginationNav
        page={participants.page}
        hasNextPage={participants.hasNextPage}
        hrefFor={(target) => `/researcher?page=${target}`}
      />
    </div>
  )
}
