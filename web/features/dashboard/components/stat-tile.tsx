import type { LucideIcon } from "lucide-react"
import Link from "next/link"

import { cn } from "@/lib/utils"

const tones = {
  sky: "bg-sky-500/12 text-sky-700 dark:text-sky-300",
  emerald: "bg-emerald-500/12 text-emerald-700 dark:text-emerald-300",
  violet: "bg-violet-500/12 text-violet-700 dark:text-violet-300",
}

export type StatTone = keyof typeof tones

export function StatTile({
  label,
  value,
  hint,
  href,
  icon: Icon,
  tone,
  alert = false,
}: {
  label: string
  value: string | number
  hint: string
  href: string
  icon: LucideIcon
  tone: StatTone
  // Colors the hint as a warning, e.g. for refused access attempts.
  alert?: boolean
}) {
  return (
    <Link
      href={href}
      className="flex items-start justify-between gap-3 rounded-xl border bg-card p-5 transition-colors hover:bg-muted/50 focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none"
    >
      <div className="flex flex-col gap-1">
        <span className="text-sm text-muted-foreground">{label}</span>
        <span className="text-3xl font-semibold tracking-tight">{value}</span>
        <span
          className={cn(
            "text-xs",
            alert
              ? "font-medium text-red-700 dark:text-red-300"
              : "text-muted-foreground"
          )}
        >
          {hint}
        </span>
      </div>
      <div
        className={cn(
          "flex size-9 shrink-0 items-center justify-center rounded-lg",
          tones[tone]
        )}
      >
        <Icon className="size-5" aria-hidden />
      </div>
    </Link>
  )
}
