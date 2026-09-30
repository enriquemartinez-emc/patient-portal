import { PaginationNav } from "@/components/pagination-nav"
import { requireSessionPage } from "@/features/auth/session"
import { PatientsTable } from "@/features/patients/components/patients-table"
import { listMyPatients } from "@/features/patients/repository"
import { pageParam } from "@/lib/query"

export const metadata = { title: "Patients · Patient Portal" }

export default async function ClinicianPatientsPage({
  searchParams,
}: {
  searchParams: Promise<{ page?: string }>
}) {
  const session = await requireSessionPage("clinician")
  const page = pageParam.parse((await searchParams).page)
  const patients = await listMyPatients(session.clinicianId, page)

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold">Your patients</h1>
        <p className="text-sm text-muted-foreground">
          Welcome, {session.name}. Patients you treat, or who have shared their
          records with your organization.
        </p>
      </div>
      <PatientsTable patients={patients.items} />
      <PaginationNav
        page={patients.page}
        hasNextPage={patients.hasNextPage}
        hrefFor={(target) => `/clinician?page=${target}`}
      />
    </div>
  )
}
