import Link from "next/link"
import { notFound } from "next/navigation"

import { AccessDenied } from "@/components/access-denied"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Badge } from "@/components/ui/badge"
import { requireSessionPage } from "@/features/auth/session"
import { LabResultsTable } from "@/features/lab-results/components/lab-results-table"
import {
  getMyPatient,
  listPatientLabResults,
} from "@/features/patients/repository"
import { isForbidden } from "@/lib/api/server"
import { accessBasisLabels } from "@/features/patients/labels"
import { formatDate } from "@/lib/format"

export const metadata = { title: "Patient lab results · Patient Portal" }

export default async function ClinicianPatientPage({
  params,
}: {
  params: Promise<{ patientId: string }>
}) {
  const { patientId } = await params
  const session = await requireSessionPage("clinician")

  const patient = await getMyPatient(session.clinicianId, patientId)
  if (!patient) {
    notFound()
  }

  let results
  try {
    // This request is the audited read: the patient can see that it happened.
    results = await listPatientLabResults(session.clinicianId, patientId)
  } catch (error) {
    if (isForbidden(error)) {
      return <AccessDenied />
    }
    throw error
  }

  return (
    <div className="flex flex-col gap-6">
      <Link
        href="/clinician"
        className="text-sm text-muted-foreground hover:text-foreground"
      >
        ← All patients
      </Link>
      <div>
        <h1 className="text-2xl font-semibold">{patient.fullName}</h1>
        <p className="text-sm text-muted-foreground">
          Born {formatDate(patient.dateOfBirth)}
        </p>
        <div className="mt-2 flex flex-wrap gap-2">
          {patient.accessBasis.map((basis) => (
            <Badge key={basis} variant="outline">
              {accessBasisLabels[basis]}
            </Badge>
          ))}
        </div>
      </div>
      <Alert>
        <AlertTitle>This access is recorded</AlertTitle>
        <AlertDescription>
          Opening these results is added to {patient.fullName}&apos;s access
          history, which they can see.
        </AlertDescription>
      </Alert>
      <LabResultsTable results={results} />
    </div>
  )
}
