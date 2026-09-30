import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { activityByDay } from "@/features/audit/activity"
import { listAuditTrail } from "@/features/audit/repository"
import { requireSessionPage } from "@/features/auth/session"
import { listConsents } from "@/features/consents/repository"
import { AccessActivityChart } from "@/features/dashboard/components/access-activity-chart"
import { LatestResultsCard } from "@/features/dashboard/components/latest-results-card"
import { RecentAccessCard } from "@/features/dashboard/components/recent-access-card"
import { SharingCard } from "@/features/dashboard/components/sharing-card"
import { StatTile } from "@/features/dashboard/components/stat-tile"
import { listLabResults } from "@/features/lab-results/repository"
import { formatDate } from "@/lib/format"

export const metadata = { title: "Dashboard · Patient Portal" }

const ACTIVITY_DAYS = 14
const RECENT_DAYS = 30
const DAY_MS = 24 * 60 * 60 * 1000

export default async function PatientDashboardPage() {
  const session = await requireSessionPage("patient")

  const [results, consents, audit] = await Promise.all([
    listLabResults(session.patientId),
    listConsents(session.patientId),
    listAuditTrail(session.patientId, { page: 1, pageSize: 100 }),
  ])

  const now = new Date()
  const active = consents.filter((consent) => consent.status === "active")
  const categories = new Set(results.map((result) => result.category)).size

  const since = now.getTime() - RECENT_DAYS * DAY_MS
  const recent = audit.items.filter(
    (entry) => new Date(entry.occurredAt).getTime() >= since
  )
  const views = recent.filter(
    (entry) => entry.action.type === "lab_results_read"
  ).length
  const refused = recent.filter(
    (entry) => entry.action.type === "access_denied"
  ).length
  // Only the newest 100 entries are loaded, so a busy month can be undercounted.
  const truncated =
    audit.hasNextPage &&
    new Date(audit.items[audit.items.length - 1].occurredAt).getTime() >= since

  const [firstShared, ...otherShared] = active
  const sharedHint = !firstShared
    ? "Not shared with anyone"
    : otherShared.length === 0
      ? firstShared.granteeName
      : `${firstShared.granteeName} and ${otherShared.length} more`

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold">
          Welcome back, {session.name.split(" ")[0]}
        </h1>
        <p className="text-sm text-muted-foreground">
          Your lab results, and who can see them.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-12">
        <div className="grid grid-cols-2 gap-4 md:col-span-2 lg:col-span-12 lg:grid-cols-4">
          <StatTile
            label="Lab results"
            value={results.length}
            hint={
              results.length === 0
                ? "None yet"
                : `Latest collected ${formatDate(results[0].collectedAt)}`
            }
            href="/patient/lab-results"
          />
          <StatTile
            label="Kinds of test"
            value={categories}
            hint="Across your results"
            href="/patient/lab-results"
          />
          <StatTile
            label="Shared with"
            value={active.length}
            hint={sharedHint}
            href="/patient/consents"
          />
          <StatTile
            label={`Views in ${RECENT_DAYS} days`}
            value={`${views}${truncated ? "+" : ""}`}
            hint={
              refused > 0
                ? `${refused} refused ${refused === 1 ? "attempt" : "attempts"}`
                : "Every view is recorded"
            }
            href="/patient/access-history"
          />
        </div>

        <LatestResultsCard
          results={results.slice(0, 5)}
          className="md:col-span-2 lg:col-span-7"
        />

        <Card className="md:col-span-2 lg:col-span-5">
          <CardHeader>
            <CardTitle>Who has opened your results</CardTitle>
            <CardDescription>Last {ACTIVITY_DAYS} days</CardDescription>
          </CardHeader>
          <CardContent>
            <AccessActivityChart
              days={activityByDay(audit.items, ACTIVITY_DAYS, now)}
            />
          </CardContent>
        </Card>

        <SharingCard
          consents={active}
          className="md:col-span-1 lg:col-span-6"
        />
        <RecentAccessCard
          entries={audit.items.slice(0, 5)}
          className="md:col-span-1 lg:col-span-6"
        />
      </div>
    </div>
  )
}
