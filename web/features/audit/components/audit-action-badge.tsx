import { Badge } from "@/components/ui/badge"
import { auditActionLabels } from "@/features/audit/components/audit-table"
import type { AuditActionType } from "@/features/audit/types"
import { cn } from "@/lib/utils"

const actionTones: Record<AuditActionType, string> = {
  lab_results_read: "bg-sky-500/12 text-sky-700 dark:text-sky-300",
  consent_granted: "bg-emerald-500/12 text-emerald-700 dark:text-emerald-300",
  consent_revoked: "bg-amber-500/15 text-amber-700 dark:text-amber-300",
  access_denied: "bg-red-500/12 text-red-700 dark:text-red-300",
}

export function AuditActionBadge({ type }: { type: AuditActionType }) {
  return (
    <Badge
      variant="outline"
      className={cn("border-transparent", actionTones[type])}
    >
      {auditActionLabels[type]}
    </Badge>
  )
}
