import {
  Card,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { listLabResults } from "@/features/lab-results/repository"
import { requireSessionPage } from "@/features/auth/session"

export default async function PatientHomePage() {
  const session = await requireSessionPage("patient")
  const results = await listLabResults(session.patientId)

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-semibold">Welcome, {session.name}</h1>
      <Card>
        <CardHeader>
          <CardTitle>{results.length} lab results on record</CardTitle>
          <CardDescription>
            Results, consents and your access history will appear here.
          </CardDescription>
        </CardHeader>
      </Card>
    </div>
  )
}
