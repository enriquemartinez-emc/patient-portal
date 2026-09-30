import { PaginationNav } from "@/components/pagination-nav"
import { Button } from "@/components/ui/button"
import { Label } from "@/components/ui/label"
import { NativeSelect, NativeSelectOption } from "@/components/ui/native-select"
import {
  auditActionLabel,
  parseAuditActionFilter,
  parsePageNumber,
} from "@/core/audit/audit.rules"
import { AUDIT_ACTION_TYPES } from "@/core/audit/audit.types"
import { AuditTable } from "@/features/audit/components/audit-table"
import { listAuditTrail } from "@/features/audit/repository"
import { requireSessionPage } from "@/features/auth/session"

export const metadata = { title: "Access history · Patient Portal" }

export default async function AccessHistoryPage({
  searchParams,
}: {
  searchParams: Promise<{ page?: string; action?: string }>
}) {
  const session = await requireSessionPage("patient")
  const params = await searchParams
  const page = parsePageNumber(params.page)
  const action = parseAuditActionFilter(params.action)

  const trail = await listAuditTrail(session.patientId, { page, action })

  const hrefFor = (target: number) => {
    const query = new URLSearchParams({ page: String(target) })
    if (action) {
      query.set("action", action)
    }
    return `/patient/access-history?${query}`
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold">Access history</h1>
        <p className="text-sm text-muted-foreground">
          Everyone who has viewed your results, and every change you made to
          your consents. This record can&apos;t be edited.
        </p>
      </div>

      <form method="get" className="flex flex-wrap items-end gap-3">
        <div className="flex flex-col gap-2">
          <Label htmlFor="action">Show</Label>
          <NativeSelect id="action" name="action" defaultValue={action ?? ""}>
            <NativeSelectOption value="">Everything</NativeSelectOption>
            {AUDIT_ACTION_TYPES.map((type) => (
              <NativeSelectOption key={type} value={type}>
                {auditActionLabel(type)}
              </NativeSelectOption>
            ))}
          </NativeSelect>
        </div>
        <Button type="submit" variant="outline">
          Apply
        </Button>
      </form>

      <AuditTable entries={trail.items} />
      <PaginationNav
        page={trail.page}
        hasNextPage={trail.hasNextPage}
        hrefFor={hrefFor}
      />
    </div>
  )
}
