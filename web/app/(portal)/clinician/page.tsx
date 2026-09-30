import { Badge } from "@/components/ui/badge"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { listMyPatients } from "@/features/patients/api"
import { requireSessionPage } from "@/shell/session"

export default async function ClinicianHomePage() {
  const session = await requireSessionPage("clinician")
  const { items } = await listMyPatients(session.clinicianId)

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-semibold">Welcome, {session.name}</h1>
      <Card>
        <CardHeader>
          <CardTitle>Your patients</CardTitle>
          <CardDescription>
            Patients you treat, or who have shared their records with your
            organization.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <ul className="flex flex-col gap-2">
            {items.map((patient) => (
              <li key={patient.id} className="flex items-center gap-3">
                <span>{patient.fullName}</span>
                {patient.accessBasis.map((basis) => (
                  <Badge key={basis} variant="outline">
                    {basis}
                  </Badge>
                ))}
              </li>
            ))}
            {items.length === 0 && (
              <li className="text-muted-foreground">No patients yet.</li>
            )}
          </ul>
        </CardContent>
      </Card>
    </div>
  )
}
