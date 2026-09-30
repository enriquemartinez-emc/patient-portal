import Link from "next/link"

import { Badge } from "@/components/ui/badge"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { accessBasisLabel } from "@/core/patients/patients.rules"
import type { PatientSummary } from "@/core/patients/patients.types"
import { formatDate } from "@/core/shared/dates"

export function PatientsTable({
  patients,
}: {
  patients: readonly PatientSummary[]
}) {
  if (patients.length === 0) {
    return (
      <p className="text-muted-foreground">
        You have no patients yet. Patients appear here when you treat them or
        they share their records with your organization.
      </p>
    )
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Patient</TableHead>
          <TableHead>Date of birth</TableHead>
          <TableHead>Why you can see them</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {patients.map((patient) => (
          <TableRow key={patient.id}>
            <TableCell>
              {/* No prefetch: opening a patient's results is recorded in their audit trail. */}
              <Link
                href={`/clinician/patients/${patient.id}`}
                prefetch={false}
                className="font-medium underline-offset-4 hover:underline"
              >
                {patient.fullName}
              </Link>
            </TableCell>
            <TableCell>{formatDate(patient.dateOfBirth)}</TableCell>
            <TableCell>
              <div className="flex flex-wrap gap-2">
                {patient.accessBasis.map((basis) => (
                  <Badge key={basis} variant="outline">
                    {accessBasisLabel(basis)}
                  </Badge>
                ))}
              </div>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}
