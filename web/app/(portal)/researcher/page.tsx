import {
  Card,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { requireSessionPage } from "@/shell/session"

export default async function ResearcherHomePage() {
  const session = await requireSessionPage("researcher")

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-semibold">Welcome, {session.name}</h1>
      <Card>
        <CardHeader>
          <CardTitle>Consented datasets</CardTitle>
          <CardDescription>
            Lab results appear here for patients who have shared the relevant
            categories with your institution.
          </CardDescription>
        </CardHeader>
      </Card>
    </div>
  )
}
