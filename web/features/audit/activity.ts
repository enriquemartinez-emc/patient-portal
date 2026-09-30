import type { AuditEntry } from "@/features/audit/types"

export type DayActivity = { date: string; views: number }

const DAY_MS = 24 * 60 * 60 * 1000

// How many times the patient's results were opened on each of the last `days` days (UTC), oldest
// first. Every day in the range is present, with 0 when nothing happened.
export function activityByDay(
  entries: readonly AuditEntry[],
  days: number,
  now: Date
): DayActivity[] {
  const today = Date.UTC(
    now.getUTCFullYear(),
    now.getUTCMonth(),
    now.getUTCDate()
  )
  const dates = Array.from({ length: days }, (_, index) =>
    new Date(today - (days - 1 - index) * DAY_MS).toISOString().slice(0, 10)
  )
  const views = new Map(dates.map((date) => [date, 0]))

  for (const entry of entries) {
    if (entry.action.type !== "lab_results_read") {
      continue
    }
    const date = new Date(entry.occurredAt).toISOString().slice(0, 10)
    if (views.has(date)) {
      views.set(date, views.get(date)! + 1)
    }
  }

  return dates.map((date) => ({ date, views: views.get(date)! }))
}
